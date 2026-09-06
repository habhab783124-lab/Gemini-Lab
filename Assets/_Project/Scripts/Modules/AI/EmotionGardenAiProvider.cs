#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Tarot;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.AI
{
    /// <summary>
    /// 情绪花园的 OpenAI 兼容 AI 提供器。
    /// 连接配置复用室内聊天使用的 Resources/LLMConfig.asset；未配置或失败时返回 null，
    /// 由 EmotionGardenService 负责本地兜底和最终提交。
    /// </summary>
    public sealed class EmotionGardenAiProvider : IEmotionGardenAiProvider
    {
        private readonly LLMConfigSO _config;

        public EmotionGardenAiProvider(LLMConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<EmotionGardenAiResult?> GenerateAsync(
            string dateIso,
            string inputSentence,
            string emotionType,
            string owner,
            string flowerName,
            string flowerDescription,
            CancellationToken cancellationToken = default)
        {
            if (!_config.IsConfigured)
            {
                return null;
            }

            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(
                dateIso,
                inputSentence,
                emotionType,
                owner,
                flowerName,
                flowerDescription);

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                float timeoutSeconds = _config.TimeoutSeconds > 0f ? _config.TimeoutSeconds : 10f;
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                string response = await SendRequestAsync(systemPrompt, userPrompt, timeoutCts.Token);
                return ParseResponse(response);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                Debug.LogWarning("[EmotionGardenAI] 请求超时，使用本地兜底。");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EmotionGardenAI] 请求失败，使用本地兜底：{ex.Message}");
                return null;
            }
        }

        private static string BuildSystemPrompt()
        {
            return "你是情绪花园的内容生成器。只返回 JSON，不要 markdown，不要解释。" +
                   "字段必须为 EmotionType、EmotionKeywords、FlowerDescription、FlowerLanguage、Summary、AngelNote、DevilNote。" +
                   "EmotionType 只能是：喜悦、悲伤、愤怒、平静、爱、恐惧、惊讶、期待、孤独。" +
                   "EmotionKeywords 返回 2 到 5 个简短中文关键词。" +
                   "Summary 必须 30 到 60 个中文字符；AngelNote 和 DevilNote 必须分别 40 到 80 个中文字符。" +
                   "FlowerDescription 和 FlowerLanguage 使用温和、具体的中文，不编造不存在的花名。";
        }

        private static string BuildUserPrompt(
            string dateIso,
            string inputSentence,
            string emotionType,
            string owner,
            string flowerName,
            string flowerDescription)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"日期：{dateIso}");
            sb.AppendLine($"玩家输入：{inputSentence}");
            sb.AppendLine($"本地候选情绪：{emotionType}");
            sb.AppendLine($"培育者：{EmotionFlowerCatalog.ResolveOwnerDisplayName(owner)}");
            sb.AppendLine($"已映射花名：{flowerName}");
            sb.AppendLine($"花朵基础描述：{flowerDescription}");
            sb.AppendLine("请基于以上内容判断最终情绪，并生成每日小结和每周培育所需文本。");
            return sb.ToString();
        }

        private async Task<string> SendRequestAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            var body = new LlmRequest
            {
                model = _config.Model,
                messages = new[]
                {
                    new LlmMessage { role = "system", content = systemPrompt },
                    new LlmMessage { role = "user", content = userPrompt }
                },
                max_tokens = 420
            };

            string json = JsonUtility.ToJson(body);
            using var request = new UnityWebRequest(_config.Endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"LLM request failed: {request.error}");
            }

            string responseJson = request.downloadHandler?.text ?? string.Empty;
            var response = JsonUtility.FromJson<LlmResponse>(responseJson);
            if (response?.choices == null || response.choices.Length == 0)
            {
                throw new InvalidOperationException("LLM response has no choices");
            }

            LlmMessage? message = response.choices[0].message;
            if (message == null)
            {
                throw new InvalidOperationException("LLM response message is empty");
            }

            return message.content ?? string.Empty;
        }

        private static EmotionGardenAiResult ParseResponse(string response)
        {
            string cleaned = response.Trim();
            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                int firstLineEnd = cleaned.IndexOf('\n');
                int lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                {
                    cleaned = cleaned.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
                }
            }

            var result = JsonUtility.FromJson<EmotionGardenAiResult>(cleaned);
            if (result == null)
            {
                throw new InvalidOperationException("AI content JSON is empty");
            }

            result.EmotionKeywords = EmotionGardenAiValidation.NormalizeKeywords(result.EmotionKeywords);
            return result;
        }

        [Serializable]
        private sealed class LlmRequest
        {
            public string model = string.Empty;
            public LlmMessage[] messages = Array.Empty<LlmMessage>();
            public int max_tokens;
        }

        [Serializable]
        private sealed class LlmMessage
        {
            public string role = string.Empty;
            public string content = string.Empty;
        }

        [Serializable]
        private sealed class LlmResponse
        {
            public LlmChoice[] choices = Array.Empty<LlmChoice>();
        }

        [Serializable]
        private sealed class LlmChoice
        {
            public LlmMessage? message;
        }
    }
}
