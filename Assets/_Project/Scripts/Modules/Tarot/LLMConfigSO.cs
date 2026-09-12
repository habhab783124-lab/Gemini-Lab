#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// LLM 直连配置：endpoint / key / model / prompt 模板。
    /// 在 Project 窗口右键 Create → GeminiLab → Tarot → LLM Config 创建。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/LLM Config", fileName = "LLMConfig")]
    public sealed class LLMConfigSO : ScriptableObject
    {
        [Tooltip("OpenAI 兼容 API 地址，如 https://api.openai.com/v1/chat/completions")]
        [SerializeField] private string _endpoint = "https://api.openai.com/v1/chat/completions";

        [Tooltip("API Key")]
        [SerializeField] private string _apiKey = string.Empty;

        [Tooltip("首选模型名，如 deepseek-v4-pro")]
        [SerializeField] private string _model = "gpt-4o";

        [Tooltip("首选模型无有效返回时按顺序尝试的备用模型列表")]
        [SerializeField] private string[] _fallbackModels = { "deepseek-v4-flash" };

        [Tooltip("天使 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _angelSystemTemplate =
            "你是一位天使 (Angel) —— 温柔、包容、愿意指出希望。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 100 个汉字。";

        [Tooltip("恶魔 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _devilSystemTemplate =
            "你是一位恶魔 (Devil) —— 尖锐、坦白、敢把阴影讲透。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 100 个汉字。回答要带戏剧性但不恶毒。";

        [Tooltip("User 消息模板。占位符: {cardName}, {slotName}, {question}, {keywords}")]
        [TextArea(3, 10)]
        [SerializeField] private string _userMessageTemplate =
            "玩家抽到了：{cardName}。这是代表「{slotName}」的牌。\n" +
            "玩家想问：{question}\n" +
            "关键词：{keywords}\n" +
            "请从你的人格视角给出「{slotName}」的解读。";

        [Tooltip("总结轮 System prompt。占位符: {pastCard}, {presentCard}, {futureCard}, {question}")]
        [TextArea(5, 20)]
        [SerializeField] private string _summarySystemTemplate =
            "你是一位塔罗占卜师，已为玩家解读了过去、现在、未来三张牌。\n" +
            "请基于这三张牌给出综合运势总结，严格返回以下 JSON 格式（不要包含任何其他文字）：\n" +
            "{\n" +
            "  \"fortuneLevel\": <1-5的整数，代表运势星级>,\n" +
            "  \"luckyHint\": {\n" +
            "    \"color\": \"<幸运颜色>\",\n" +
            "    \"number\": \"<幸运数字>\",\n" +
            "    \"time\": \"<幸运时间段>\",\n" +
            "    \"action\": \"<幸运行动建议>\"\n" +
            "  },\n" +
            "  \"advice\": \"<今日综合建议，2-3句话>\"\n" +
            "}\n" +
            "过去牌：{pastCard}\n" +
            "现在牌：{presentCard}\n" +
            "未来牌：{futureCard}\n" +
            "玩家问题：{question}";

        [Tooltip("单次请求超时秒数")]
        [SerializeField] private float _timeoutSeconds = 30f;

        public string Endpoint => _endpoint;
        public string ApiKey => _apiKey;
        public string Model => _model;
        public string[] ModelCandidates
        {
            get
            {
                var candidates = new List<string>();
                AddModelCandidate(candidates, _model);
                if (_fallbackModels != null)
                {
                    foreach (string fallbackModel in _fallbackModels)
                    {
                        AddModelCandidate(candidates, fallbackModel);
                    }
                }

                return candidates.ToArray();
            }
        }
        public string AngelSystemTemplate => _angelSystemTemplate;
        public string DevilSystemTemplate => _devilSystemTemplate;
        public string UserMessageTemplate => _userMessageTemplate;
        public string SummarySystemTemplate => _summarySystemTemplate;
        public float TimeoutSeconds => _timeoutSeconds;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey);

        private static void AddModelCandidate(List<string> candidates, string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return;

            string normalized = model.Trim();
            if (!candidates.Contains(normalized))
            {
                candidates.Add(normalized);
            }
        }
    }
}
