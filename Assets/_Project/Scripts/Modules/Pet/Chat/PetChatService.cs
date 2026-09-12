#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.Pet.Personality;
using GeminiLab.Modules.Tarot;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.Pet
{
    public interface IPetChatService
    {
        Task<PetChatResult> SendMessageAsync(
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default);
    }

    public sealed class PetChatResult
    {
        public string AngelReply = string.Empty;
        public string DevilReply = string.Empty;
        public bool IsAngelFallback;
        public bool IsDevilFallback;
        public bool IsCancelled;
    }

    public sealed class PetChatService : IPetChatService
    {
        private readonly LLMConfigSO _config;
        private readonly float _timeoutSeconds;

        private static readonly string[] AngelFallbacks =
        {
            "我一直都在呢~有什么想聊的？🌸",
            "今天也请保持好心情哦。",
            "虽然不太确定，但我觉得一切都会好起来的~",
            "你想听听我的想法吗？☀️",
            "温柔对待自己，你已经做得很好了。"
        };

        private static readonly string[] DevilFallbacks =
        {
            "啧，又来找我了？行吧，陪你聊聊。",
            "说实话，这事有点无聊，不过你开心就好。",
            "哼，我可不是为了你才回答的。",
            "看在你主动找我的份上，勉为其难告诉你吧。",
            "直接说吧，别拐弯抹角的。"
        };

        public PetChatService(LLMConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timeoutSeconds = config.TimeoutSeconds > 0f ? config.TimeoutSeconds : 10f;
            ServiceLocator.Register<IPetChatService>(this);
        }

        public async Task<PetChatResult> SendMessageAsync(
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new PetChatResult();
            }

            bool hasAngel = false;
            bool hasDevil = false;
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                hasAngel = roster!.TryGet(PetId.Angel) != null;
                hasDevil = roster!.TryGet(PetId.Devil) != null;
            }

            if (!hasAngel && !hasDevil)
            {
                Debug.Log("[PetChat] No pets in roster — both are away");
                return new PetChatResult
                {
                    AngelReply = "（宠物不在家...）",
                    DevilReply = "（宠物不在家...）",
                    IsAngelFallback = true,
                    IsDevilFallback = true
                };
            }

            bool angelFirst = UnityEngine.Random.value >= 0.5f;

            var result = new PetChatResult();

            var firstPet = angelFirst ? PetId.Angel : PetId.Devil;
            var secondPet = angelFirst ? PetId.Devil : PetId.Angel;

            string? firstReply = null;

            if (TryGetPet(firstPet, out _))
            {
                (string reply, bool isFallback) = await RequestPetReplyAsync(
                    firstPet, userMessage, history, null, cancellationToken);
                firstReply = reply;
                SetResult(result, firstPet, reply, isFallback);
            }

            if (TryGetPet(secondPet, out _))
            {
                (string reply, bool isFallback) = await RequestPetReplyAsync(
                    secondPet, userMessage, history, firstReply, cancellationToken);
                SetResult(result, secondPet, reply, isFallback);
            }

            return result;
        }

        private async Task<(string reply, bool isFallback)> RequestPetReplyAsync(
            PetId petId,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            string? otherPetReply,
            CancellationToken cancellationToken)
        {
            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(userMessage, otherPetReply, history);

            if (!_config.IsConfigured)
            {
                Debug.Log($"[PetChat] LLM not configured for {petId}, using fallback");
                return (GetFallback(petId), true);
            }

            string[] modelCandidates = _config.ModelCandidates;
            if (modelCandidates.Length == 0)
            {
                LogFallback(petId, "NoModelCandidates");
                return (GetFallback(petId), true);
            }

            Debug.Log($"[AI] request-entry role={petId} feature=PetChat service={GetType().Name} registered=true models={string.Join(",", modelCandidates)}");
            string lastFailure = string.Empty;
            for (int modelIndex = 0; modelIndex < modelCandidates.Length; modelIndex++)
            {
                string model = modelCandidates[modelIndex];
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                    string response = await SendLLMRequestAsync(systemPrompt, userPrompt, model, cts.Token);
                    string preview = string.IsNullOrWhiteSpace(response)
                        ? "<empty>"
                        : response[..Math.Min(response.Length, 50)];
                    Debug.Log($"[AI] raw-response role={petId} feature=PetChat model={model} content={SanitizeForLog(preview)}");
                    string cleaned = CleanResponse(response);
                    if (string.IsNullOrWhiteSpace(cleaned))
                    {
                        lastFailure = "EmptyResponse";
                        LogModelFailure(petId, model, modelIndex, modelCandidates.Length, lastFailure);
                        continue;
                    }

                    Debug.Log($"[AI] result-accepted role={petId} feature=PetChat model={model}");
                    return (cleaned, false);
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested) throw;
                    lastFailure = "Timeout";
                    LogModelFailure(petId, model, modelIndex, modelCandidates.Length, lastFailure);
                }
                catch (Exception ex)
                {
                    lastFailure = $"{ex.GetType().Name}:{SanitizeForLog(ex.Message, 240)}";
                    LogModelFailure(petId, model, modelIndex, modelCandidates.Length, lastFailure);
                }
            }

            LogFallback(petId, $"AllModelsFailed:{lastFailure}");
            return (GetFallback(petId), true);
        }

        private string BuildSystemPrompt(PetId petId)
        {
            string petName = petId == PetId.Angel ? "Angel" : "Devil";

            PersonalityVector personality = default;
            if (ServiceLocator.TryResolve<IPersonalityEvolutionService>(out var personalityService))
            {
                personality = personalityService!.GetMatrix(petId);
            }

            PetRuntimeData? runtime = null;
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                runtime = roster!.TryGet(petId);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"你是 {petName}，住在玩家的公寓里。");
            sb.AppendLine();
            sb.AppendLine("## 你的性格");
            sb.AppendLine($"- 善良: {personality.Kindness:F2}  /  邪恶: {personality.Evilness:F2}  /  冷静: {personality.Calmness:F2}");
            sb.AppendLine($"- 勇敢: {personality.Bravery:F2}  /  害羞: {personality.Shyness:F2}  /  正直: {personality.Integrity:F2}");
            sb.AppendLine($"- 好奇心: {personality.Curiosity:F2}");

            if (runtime != null)
            {
                sb.AppendLine();
                sb.AppendLine("## 你当前的状态");
                sb.AppendLine($"- 心情: {runtime.Mood:F0}/100");
                sb.AppendLine($"- 精力: {runtime.Energy:F0}/100");
                sb.AppendLine($"- 饱腹: {runtime.Satiety:F0}/100");
                sb.AppendLine($"- 当前动作: {runtime.CurrentState}");
                if (runtime.IsTraveling)
                {
                    sb.AppendLine("- 正在旅行中");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## 回复规则");
            sb.AppendLine("- 回复要简短自然，2-4句话，不要超过 60 个字");
            sb.AppendLine("- 回复要符合你的性格，保持角色一致性");
            sb.AppendLine("- 回复可以提及你当前的状态（比如累了就说累）");
            sb.AppendLine("- 用口语化中文回复");
            sb.AppendLine("- 可以适当接上一句话茬（如果另一个宠物刚说过话）");
            sb.AppendLine("- 不要使用任何 markup 或 JSON，只输出纯文本回复");

            return sb.ToString();
        }

        private static string BuildUserPrompt(string userMessage, string? otherPetReply,
            IReadOnlyList<ChatMessage> history)
        {
            var sb = new StringBuilder();

            // Include recent conversation history (last 10 turns) for context
            int start = Math.Max(0, history.Count - 10);
            for (int i = start; i < history.Count; i++)
            {
                var msg = history[i];
                string roleLabel = msg.Role switch
                {
                    ChatRole.User => "用户",
                    ChatRole.Angel => "Angel",
                    ChatRole.Devil => "Devil",
                    _ => "?"
                };
                sb.AppendLine($"{roleLabel}：{msg.Text}");
            }

            if (!string.IsNullOrEmpty(otherPetReply))
            {
                sb.AppendLine($"[另一个宠物刚对你说：{otherPetReply}]");
            }

            sb.AppendLine($"用户说：{userMessage}");
            return sb.ToString();
        }

        private async Task<string> SendLLMRequestAsync(
            string systemPrompt, string userPrompt, string model, CancellationToken cancellationToken)
        {
            var body = new LLMRequest
            {
                model = model,
                messages = new[]
                {
                    new LLMMessage { role = "system", content = systemPrompt },
                    new LLMMessage { role = "user", content = userPrompt }
                },
                max_tokens = 180
            };

            string json = JsonUtility.ToJson(body);
            using var req = new UnityWebRequest(_config.Endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = req.SendWebRequest();
            Debug.Log($"[AI] request-sent role=PetChat feature=PetChat method=POST endpoint={_config.Endpoint} model={model}");
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

        private void LogModelFailure(PetId petId, string model, int modelIndex, int modelCount, string reason)
        {
            bool hasNextModel = modelIndex + 1 < modelCount;
            Debug.LogWarning($"[AI] model-failed role={petId} feature=PetChat model={model} attempt={modelIndex + 1}/{modelCount} hasNext={hasNextModel} reason={SanitizeForLog(reason, 240)}");
        }

        private void LogFallback(PetId petId, string reason)
        {
            Debug.LogWarning($"[Fallback] role={petId} feature=PetChat reason={SanitizeForLog(reason, 240)}");
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

        private static string GetFallback(PetId petId)
        {
            var pool = petId == PetId.Angel ? AngelFallbacks : DevilFallbacks;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private static string CleanResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string trimmed = raw.Trim();
            if (trimmed.StartsWith("```")) trimmed = trimmed[3..];
            if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
            return trimmed.Trim();
        }

        private static void SetResult(PetChatResult result, PetId petId, string reply, bool isFallback)
        {
            if (petId == PetId.Angel)
            {
                result.AngelReply = reply;
                result.IsAngelFallback = isFallback;
            }
            else
            {
                result.DevilReply = reply;
                result.IsDevilFallback = isFallback;
            }
        }

        private static bool TryGetPet(PetId petId, out PetRuntimeData? data)
        {
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                data = roster!.TryGet(petId);
                return data != null;
            }
            data = null;
            return false;
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
