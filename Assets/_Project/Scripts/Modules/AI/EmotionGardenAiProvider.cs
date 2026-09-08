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
    /// Direct DeepSeek provider for the emotion garden. OpenClaw Gateway is not part
    /// of this path because the project uses the LLMConfig asset for direct access.
    /// </summary>
    public sealed class EmotionGardenAiProvider : IEmotionGardenAiProvider
    {
        private const int LogPreviewMaxLength = 1200;
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
            string normalizedOwner = EmotionFlowerCatalog.NormalizeOwner(owner);
            string role = ResolveLogRole(normalizedOwner);
            bool inputEmpty = string.IsNullOrWhiteSpace(inputSentence);

            if (!_config.IsConfigured)
            {
                LogFallback(role, "ConfigMissing");
                return null;
            }

            Debug.Log($"[AI] request-entry role={role} feature=Emotion inputEmpty={inputEmpty} service={GetType().Name} registered=true sent=false");

            string systemPrompt = BuildSystemPrompt(normalizedOwner);
            string userPrompt = BuildUserPrompt(
                dateIso,
                inputSentence,
                emotionType,
                normalizedOwner,
                flowerName,
                flowerDescription);

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                float timeoutSeconds = _config.TimeoutSeconds > 0f ? _config.TimeoutSeconds : 10f;
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                string responseContent = await SendRequestAsync(role, systemPrompt, userPrompt, timeoutCts.Token);
                Debug.Log($"[AI] raw-response role={role} feature=Emotion content={SanitizeForLog(responseContent)}");

                EmotionGardenAiResult result = ParseResponse(responseContent);
                if (!EmotionGardenAiValidation.IsCompleteResult(result, out string validationReason))
                {
                    LogFallback(role, $"InvalidResult:{validationReason}");
                    return null;
                }

                result.EmotionType = EmotionFlowerCatalog.NormalizeEmotionType(result.EmotionType);
                result.EmotionKeywords = EmotionGardenAiValidation.NormalizeKeywords(result.EmotionKeywords);
                result.FlowerDescription = EmotionGardenAiValidation.NormalizeText(result.FlowerDescription, 120);
                result.FlowerLanguage = EmotionGardenAiValidation.NormalizeText(result.FlowerLanguage, 160);
                result.Summary = EmotionGardenAiValidation.NormalizeText(result.Summary, 60);
                result.AngelNote = EmotionGardenAiValidation.NormalizeText(result.AngelNote, 80);
                result.DevilNote = EmotionGardenAiValidation.NormalizeText(result.DevilNote, 80);
                result.ResultSource = EmotionGardenResultSources.Ai;
                result.IsFallback = false;

                Debug.Log($"{EmotionGardenResultSources.Ai} role={role} feature=Emotion result=Accepted emotion={result.EmotionType}");
                return result;
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                LogFallback(role, "Timeout");
                return null;
            }
            catch (Exception ex)
            {
                LogFallback(role, $"{ex.GetType().Name}:{SanitizeForLog(ex.Message, 240)}");
                return null;
            }
        }

        private static string BuildSystemPrompt(string owner)
        {
            string roleInstruction = EmotionFlowerCatalog.NormalizeOwner(owner) == EmotionFlowerCatalog.OwnerDemon
                ? "当前角色是恶魔（Devil），表达要尖锐、坦率、敢于直面情绪，但不能恶意伤害玩家。"
                : "当前角色是天使（Angel），表达要温和、包容、鼓励玩家看见自己的感受。";

            return roleInstruction +
                   "你是情绪花园的 AI 内容生成器。只返回一个 JSON 对象，不要返回 Markdown、解释或额外文字。" +
                   "字段必须为 EmotionType、EmotionKeywords、FlowerDescription、FlowerLanguage、Summary、AngelNote、DevilNote。" +
                   "EmotionType 只能是：喜悦、悲伤、愤怒、平静、爱、恐惧、惊讶、期待、孤独。" +
                   "EmotionType 必须从上述列表中原样选择，禁止输出困惑、焦虑、开心、难过或任何列表外词语；列表外含义请选择最接近的一个类别。" +
                   "必须优先根据玩家输入判断，不能无理由返回平静；例如生气/愤怒优先选愤怒，开心/快乐优先选喜悦，悲伤/难过优先选悲伤，明确平静才选平静。" +
                   "EmotionKeywords 返回 2 到 5 个简短中文关键词。" +
                   "Summary 必须是 30 到 60 个中文字符；AngelNote 和 DevilNote 必须分别是 40 到 80 个中文字符。" +
                   "FlowerDescription 和 FlowerLanguage 使用具体、自然的中文。";
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
            sb.AppendLine($"当前角色：{EmotionFlowerCatalog.ResolveOwnerDisplayName(owner)}");
            sb.AppendLine($"已映射花名：{flowerName}");
            sb.AppendLine($"花朵基础描述：{flowerDescription}");
            sb.AppendLine("请结合玩家输入和当前角色上下文，返回符合约束的 JSON。Summary 是面向玩家的整体总结，AngelNote 和 DevilNote 必须分别保留各自角色的语气。");
            return sb.ToString();
        }

        private async Task<string> SendRequestAsync(
            string role,
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
                max_tokens = 420,
                temperature = 0.2f,
                thinking = new LlmThinking { type = "disabled" },
                response_format = new LlmResponseFormat { type = "json_object" }
            };

            string json = JsonUtility.ToJson(body);
            using var request = new UnityWebRequest(_config.Endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = request.SendWebRequest();
            Debug.Log($"[AI] request-sent role={role} feature=Emotion method=POST endpoint={_config.Endpoint} model={_config.Model}");
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            string responseJson = request.downloadHandler?.text ?? string.Empty;
            Debug.Log($"[AI] response-received role={role} feature=Emotion httpStatus={request.responseCode} result={request.result} bodyEmpty={string.IsNullOrWhiteSpace(responseJson)} bodyLength={responseJson.Length}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"HttpFailure:{request.result}:{request.responseCode}:{request.error}");
            }

            LlmResponse? response;
            try
            {
                response = JsonUtility.FromJson<LlmResponse>(responseJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ResponseEnvelopeParseFailed:{ex.GetType().Name}", ex);
            }

            if (response?.choices == null || response.choices.Length == 0)
            {
                throw new InvalidOperationException("ResponseEnvelopeMissingChoices");
            }

            LlmMessage? message = response.choices[0].message;
            if (message == null)
            {
                throw new InvalidOperationException("ResponseMessageMissing");
            }

            return message.content ?? string.Empty;
        }

        private static EmotionGardenAiResult ParseResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidOperationException("AiContentEmpty");
            }

            string json = ExtractJsonObject(response);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("AiContentJsonObjectMissing");
            }

            EmotionGardenAiResult? result;
            try
            {
                result = JsonUtility.FromJson<EmotionGardenAiResult>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"AiContentJsonParseFailed:{ex.GetType().Name}", ex);
            }

            if (result == null)
            {
                throw new InvalidOperationException("AiContentJsonParseFailed:NullResult");
            }

            result.EmotionKeywords = EmotionGardenAiValidation.NormalizeKeywords(result.EmotionKeywords);
            return result;
        }

        private static string ExtractJsonObject(string value)
        {
            int start = value.IndexOf('{');
            if (start < 0) return string.Empty;

            int depth = 0;
            bool inString = false;
            bool escaped = false;
            for (int index = start; index < value.Length; index++)
            {
                char current = value[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                }
                else if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return value.Substring(start, index - start + 1).Trim();
                    }
                }
            }

            return string.Empty;
        }

        private void LogFallback(string role, string reason)
        {
            Debug.LogWarning($"{EmotionGardenResultSources.Fallback} role={role} feature=Emotion reason={SanitizeForLog(reason, 240)}");
        }

        private static string ResolveLogRole(string owner)
        {
            return EmotionFlowerCatalog.NormalizeOwner(owner) == EmotionFlowerCatalog.OwnerDemon
                ? "Devil"
                : "Angel";
        }

        private string SanitizeForLog(string value, int maxLength = LogPreviewMaxLength)
        {
            string sanitized = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                sanitized = sanitized.Replace(_config.ApiKey, "[REDACTED]");
            }

            sanitized = sanitized.Replace("\r", "\\r").Replace("\n", "\\n");
            return sanitized.Length <= maxLength
                ? sanitized
                : sanitized.Substring(0, Math.Max(1, maxLength - 3)) + "...";
        }

        [Serializable]
        private sealed class LlmRequest
        {
            public string model = string.Empty;
            public LlmMessage[] messages = Array.Empty<LlmMessage>();
            public int max_tokens;
            public float temperature;
            public LlmThinking thinking = new();
            public LlmResponseFormat response_format = new();
        }

        [Serializable]
        private sealed class LlmThinking
        {
            public string type = string.Empty;
        }

        [Serializable]
        private sealed class LlmResponseFormat
        {
            public string type = string.Empty;
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
