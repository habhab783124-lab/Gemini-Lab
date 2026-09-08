#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.EmotionGarden
{
    public static class EmotionGardenResultSources
    {
        public const string Ai = "[AI]";
        public const string LocalRule = "[LocalRule]";
        public const string Mock = "[Mock]";
        public const string Fallback = "[Fallback]";
    }

    /// <summary>
    /// 一次情绪提交所需的 AI 生成结果。字段同时覆盖每日总结和每周培育信息。
    /// </summary>
    [Serializable]
    public sealed class EmotionGardenAiResult
    {
        public string EmotionType = string.Empty;
        public string[] EmotionKeywords = Array.Empty<string>();
        public string FlowerDescription = string.Empty;
        public string FlowerLanguage = string.Empty;
        public string Summary = string.Empty;
        public string AngelNote = string.Empty;
        public string DevilNote = string.Empty;
        public string ResultSource = EmotionGardenResultSources.Fallback;
        public bool IsFallback;

        public EmotionGardenAiResult Clone()
        {
            return new EmotionGardenAiResult
            {
                EmotionType = EmotionType,
                EmotionKeywords = EmotionKeywords == null
                    ? Array.Empty<string>()
                    : (string[])EmotionKeywords.Clone(),
                FlowerDescription = FlowerDescription,
                FlowerLanguage = FlowerLanguage,
                Summary = Summary,
                AngelNote = AngelNote,
                DevilNote = DevilNote,
                ResultSource = ResultSource,
                IsFallback = IsFallback
            };
        }
    }

    /// <summary>AI 返回字段的结构校验与长度归一化，避免非法内容写入存档或 UI。</summary>
    public static class EmotionGardenAiValidation
    {
        public static bool IsValidEmotion(string emotionType)
        {
            return EmotionFlowerCatalog.IsKnownEmotionType(emotionType);
        }

        public static string NormalizeText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string normalized = value.Trim();
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, Math.Max(1, maxLength - 1)) + "…";
        }

        public static string[] NormalizeKeywords(IEnumerable<string>? values, int maxCount = 5)
        {
            var result = new List<string>(maxCount);
            if (values == null) return result.ToArray();

            foreach (string? value in values)
            {
                string keyword = NormalizeText(value ?? string.Empty, 12);
                if (string.IsNullOrWhiteSpace(keyword) || result.Exists(existing => string.Equals(existing, keyword, StringComparison.Ordinal)))
                {
                    continue;
                }

                result.Add(keyword);
                if (result.Count >= maxCount) break;
            }

            return result.ToArray();
        }

        public static bool IsCompleteResult(EmotionGardenAiResult? result, out string reason)
        {
            if (result == null)
            {
                reason = "result-null";
                return false;
            }

            if (!IsValidEmotion(result.EmotionType))
            {
                reason = "invalid-emotion";
                return false;
            }

            string[] keywords = NormalizeKeywords(result.EmotionKeywords);
            if (keywords.Length < 2)
            {
                reason = "invalid-keywords";
                return false;
            }

            if (string.IsNullOrWhiteSpace(result.FlowerDescription))
            {
                reason = "empty-flower-description";
                return false;
            }

            if (string.IsNullOrWhiteSpace(result.FlowerLanguage))
            {
                reason = "empty-flower-language";
                return false;
            }

            if (string.IsNullOrWhiteSpace(result.Summary))
            {
                reason = "empty-summary";
                return false;
            }

            if (string.IsNullOrWhiteSpace(result.AngelNote))
            {
                reason = "empty-angel-note";
                return false;
            }

            if (string.IsNullOrWhiteSpace(result.DevilNote))
            {
                reason = "empty-devil-note";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
