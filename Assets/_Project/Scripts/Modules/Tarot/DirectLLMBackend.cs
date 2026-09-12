#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// UnityWebRequest 直连 OpenAI 兼容 LLM API 的塔罗解读后端。
    /// 需要 LLMConfigSO 配置 endpoint + key；未配置时回退到 LocalFallback。
    /// </summary>
    public sealed class DirectLLMBackend : ITarotReadingBackend
    {
        private readonly LLMConfigSO _config;
        private readonly Func<PetId, string>? _personalityResolver;

        public DirectLLMBackend(LLMConfigSO config, Func<PetId, string>? personalityResolver = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _personalityResolver = personalityResolver;
        }

        public async Task<TarotReading> RequestAsync(
            TarotDrawResult draw,
            PetId petId,
            TarotOrientation orientation,
            CancellationToken cancellationToken)
        {
            if (!_config.IsConfigured)
            {
                LogFallback($"role={petId} feature=TarotReading", "ConfigMissing");
                return LocalFallback.Build(draw, petId, orientation);
            }

            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(draw, petId);

            string[] modelCandidates = _config.ModelCandidates;
            if (modelCandidates.Length == 0)
            {
                LogFallback($"role={petId} feature=TarotReading", "NoModelCandidates");
                return LocalFallback.Build(draw, petId, orientation);
            }

            string lastFailure = string.Empty;
            for (int modelIndex = 0; modelIndex < modelCandidates.Length; modelIndex++)
            {
                string model = modelCandidates[modelIndex];
                try
                {
                    string responseText = await SendRequestAsync("Tarot", systemPrompt, userPrompt, model, cancellationToken)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        lastFailure = "EmptyResponse";
                        LogModelFailure($"role={petId} feature=TarotReading", model, modelIndex, modelCandidates.Length, lastFailure);
                        continue;
                    }

                    Debug.Log($"[AI] result-accepted role={petId} feature=TarotReading model={model}");
                    return new TarotReading(petId, orientation, responseText, isFromGateway: true);
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested) throw;
                    lastFailure = "Timeout";
                    LogModelFailure($"role={petId} feature=TarotReading", model, modelIndex, modelCandidates.Length, lastFailure);
                }
                catch (Exception ex)
                {
                    lastFailure = $"{ex.GetType().Name}:{SanitizeForLog(ex.Message, 240)}";
                    LogModelFailure($"role={petId} feature=TarotReading", model, modelIndex, modelCandidates.Length, lastFailure);
                }
            }

            LogFallback($"role={petId} feature=TarotReading", $"AllModelsFailed:{lastFailure}");
            return LocalFallback.Build(draw, petId, orientation);
        }

        public async Task<TarotSummaryResult> RequestSummaryAsync(
            TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
            string? question, CancellationToken cancellationToken)
        {
            if (!_config.IsConfigured)
            {
                LogFallback("role=Tarot feature=TarotSummary", "ConfigMissing");
                return TarotSummaryResult.Default();
            }

            string systemPrompt = _config.SummarySystemTemplate
                .Replace("{pastCard}", $"{past.Card.DisplayNameZh} ({past.Card.DisplayNameEn})")
                .Replace("{presentCard}", $"{present.Card.DisplayNameZh} ({present.Card.DisplayNameEn})")
                .Replace("{futureCard}", $"{future.Card.DisplayNameZh} ({future.Card.DisplayNameEn})")
                .Replace("{question}", question ?? "未指定");

            string[] modelCandidates = _config.ModelCandidates;
            if (modelCandidates.Length == 0)
            {
                LogFallback("role=Tarot feature=TarotSummary", "NoModelCandidates");
                return TarotSummaryResult.Default();
            }

            string lastFailure = string.Empty;
            for (int modelIndex = 0; modelIndex < modelCandidates.Length; modelIndex++)
            {
                string model = modelCandidates[modelIndex];
                try
                {
                    string responseText = await SendRequestAsync("Tarot", systemPrompt, "请返回 JSON。", model, cancellationToken)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        lastFailure = "EmptyResponse";
                        LogModelFailure("role=Tarot feature=TarotSummary", model, modelIndex, modelCandidates.Length, lastFailure);
                        continue;
                    }

                    TarotSummaryResult result = TarotSummaryResult.FromJson(responseText);
                    Debug.Log($"[AI] result-accepted role=Tarot feature=TarotSummary model={model}");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested) throw;
                    lastFailure = "Timeout";
                    LogModelFailure("role=Tarot feature=TarotSummary", model, modelIndex, modelCandidates.Length, lastFailure);
                }
                catch (Exception ex)
                {
                    lastFailure = $"{ex.GetType().Name}:{SanitizeForLog(ex.Message, 240)}";
                    LogModelFailure("role=Tarot feature=TarotSummary", model, modelIndex, modelCandidates.Length, lastFailure);
                }
            }

            LogFallback("role=Tarot feature=TarotSummary", $"AllModelsFailed:{lastFailure}");
            return TarotSummaryResult.Default();
        }

        private string BuildSystemPrompt(PetId petId)
        {
            string template = petId == PetId.Angel
                ? _config.AngelSystemTemplate
                : _config.DevilSystemTemplate;

            string personalityText = ResolvePersonality(petId);

            return template.Replace("{personality}", personalityText);
        }

        private string BuildUserPrompt(TarotDrawResult draw, PetId petId)
        {
            string template = _config.UserMessageTemplate;
            return template
                .Replace("{cardName}", $"{draw.Card.DisplayNameZh} ({draw.Card.DisplayNameEn})")
                .Replace("{slotName}", "")
                .Replace("{question}", "")
                .Replace("{keywords}", string.Join("、", draw.Card.GetKeywords(draw.Orientation)));
        }

        private async Task<string> SendRequestAsync(string role, string systemPrompt, string userPrompt,
            string model, CancellationToken cancellationToken)
        {
            var body = new LLMRequest
            {
                model = model,
                messages = new[]
                {
                    new LLMMessage { role = "system", content = systemPrompt },
                    new LLMMessage { role = "user", content = userPrompt }
                },
                max_tokens = 120
            };

            string json = JsonUtility.ToJson(body);
            using var req = new UnityWebRequest(_config.Endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = req.SendWebRequest();
            Debug.Log($"[AI] request-sent role={role} feature=Tarot method=POST endpoint={_config.Endpoint} model={model}");
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    req.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"LLM request failed: {req.error} — {req.downloadHandler?.text}");
            }

            string responseJson = req.downloadHandler?.text ?? string.Empty;
            var response = JsonUtility.FromJson<LLMResponse>(responseJson);
            if (response.choices == null || response.choices.Length == 0)
            {
                throw new Exception("LLM response has no choices");
            }

            return response.choices[0].message?.content ?? string.Empty;
        }

        private void LogModelFailure(string roleAndFeature, string model, int modelIndex, int modelCount, string reason)
        {
            bool hasNextModel = modelIndex + 1 < modelCount;
            Debug.LogWarning($"[AI] model-failed {roleAndFeature} model={model} attempt={modelIndex + 1}/{modelCount} hasNext={hasNextModel} reason={SanitizeForLog(reason, 240)}");
        }

        private void LogFallback(string roleAndFeature, string reason)
        {
            Debug.LogWarning($"[Fallback] {roleAndFeature} reason={SanitizeForLog(reason, 240)}");
        }

        private string SanitizeForLog(string value, int maxLength = 240)
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

        private string ResolvePersonality(PetId petId)
        {
            if (_personalityResolver != null)
            {
                return _personalityResolver(petId);
            }
            return "性格数据未加载";
        }

        [Serializable]
        private sealed class LLMRequest
        {
            public string model = string.Empty;
            public LLMMessage[] messages = Array.Empty<LLMMessage>();
            public int max_tokens;
        }

        [Serializable]
        private sealed class LLMMessage
        {
            public string role = string.Empty;
            public string content = string.Empty;
        }

        [Serializable]
        private sealed class LLMResponse
        {
            public LLMChoice[] choices = Array.Empty<LLMChoice>();
        }

        [Serializable]
        private sealed class LLMChoice
        {
            public LLMMessage? message;
        }
    }
}
