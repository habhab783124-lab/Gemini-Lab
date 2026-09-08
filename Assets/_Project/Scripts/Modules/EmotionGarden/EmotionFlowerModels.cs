#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.EmotionGarden
{
    public enum GrowthState
    {
        Growing = 0,
        Bloomed = 1
    }

    /// <summary>
    /// 一朵情绪花的全部数据。以 DateIso + Owner 确定唯一性。
    /// </summary>
    [Serializable]
    public struct EmotionFlowerData
    {
        public string FlowerId;
        public string DateIso;
        public int WeekId;
        public string EmotionType;
        public string FlowerName;
        public string EmotionDetail;
        public string[] EmotionKeywords;
        public string FlowerDescription;
        public string FlowerLanguage;
        public string Owner;
        public GrowthState State;
        public bool IsCollected;
        public long CreatedAtUtcTicks;
    }

    /// <summary>
    /// 按“情绪类型 + 培育者”累计的解锁进度。
    /// </summary>
    [Serializable]
    public struct ClusterProgress
    {
        public string EmotionType;
        public string Owner;
        public int TotalCount;
        public int UnlockedStage;
    }

    /// <summary>
    /// 可用于世界地图自由摆放的花卉库存。
    /// 同一情绪类型在天使与恶魔侧分别独立计数。
    /// </summary>
    [Serializable]
    public struct PlacementFlowerInventory
    {
        public string EmotionType;
        public string Owner;
        public int SingleCount;
        public int ClusterCount;
    }

    /// <summary>
    /// 一朵已经摆放到 WorldMap 的花卉记录。
    /// SlotIndex 对应 Scene 中稳定作者化的 PlacementSlot；PlacementLayerId 记录 BaselineItem 层，
    /// 世界坐标只保存运行态布局，
    /// 不写回 Scene 或 ScriptableObject。
    /// </summary>
    [Serializable]
    public struct PlacedEmotionFlower
    {
        public int SlotIndex;
        public string EmotionType;
        public string Owner;
        public bool IsCluster;
        public float WorldX;
        public float WorldY;
        public string PlacementLayerId;
    }

    /// <summary>
    /// AI 每日小结的持久化结果。它与当天的情绪花绑定，但单独保存，
    /// 这样邮箱展示不会依赖当前场景是否仍然打开。
    /// </summary>
    [Serializable]
    public struct EmotionDailySummaryData
    {
        public string DateIso;
        public string InputSentence;
        public string EmotionType;
        public string FlowerName;
        public string FlowerDescription;
        public string Summary;
        public string AngelNote;
        public string DevilNote;
        public long GeneratedAtUtcTicks;
    }

    /// <summary>情绪花提交成功。</summary>
    public readonly struct EmotionFlowerSubmittedEvent
    {
        public EmotionFlowerSubmittedEvent(EmotionFlowerData flower) { Flower = flower; }
        public EmotionFlowerData Flower { get; }
    }

    /// <summary>情绪花开花。</summary>
    public readonly struct EmotionFlowerBloomedEvent
    {
        public EmotionFlowerBloomedEvent(string flowerId) { FlowerId = flowerId; }
        public string FlowerId { get; }
    }

    /// <summary>某一种花的自由摆放库存已变化。</summary>
    public readonly struct EmotionFlowerPlacementInventoryChangedEvent
    {
        public EmotionFlowerPlacementInventoryChangedEvent(PlacementFlowerInventory inventory)
        {
            Inventory = inventory;
        }

        public PlacementFlowerInventory Inventory { get; }
    }

    /// <summary>WorldMap 已摆放花卉集合发生变化或刚从存档恢复。</summary>
    public readonly struct EmotionFlowerPlacementsChangedEvent
    {
    }

    /// <summary>[调试] 花园数据已整体清空。</summary>
    public readonly struct EmotionGardenClearedEvent
    {
    }

    /// <summary>某个情绪+培育者组合达到新解锁阶段。</summary>
    public readonly struct ClusterUnlockedEvent
    {
        public ClusterUnlockedEvent(string emotionType, string owner, int newStage)
        {
            EmotionType = emotionType;
            Owner = owner;
            NewStage = newStage;
        }

        public string EmotionType { get; }
        public string Owner { get; }
        public int NewStage { get; }
    }

    /// <summary>
    /// 情绪花的本地目录与轻量判定表。真实 AI 接入前，种植流程先用这张表稳定跑通。
    /// </summary>
    public static class EmotionFlowerCatalog
    {
        public const string OwnerAngel = "angel";
        public const string OwnerDemon = "demon";
        public const string DefaultEmotionType = "平静";

        private static readonly string[] EmotionOrder =
        {
            "喜悦",
            "悲伤",
            "愤怒",
            "平静",
            "爱",
            "恐惧",
            "惊讶",
            "期待",
            "孤独",
        };

        private static readonly Dictionary<string, EmotionDefinition> Definitions = new(StringComparer.Ordinal)
        {
            ["喜悦"] = new("喜悦", "日轮", "花火",
                "喜悦", "开心", "高兴", "快乐", "愉快", "欢喜", "兴奋", "满足", "激动", "爽"),
            ["悲伤"] = new("悲伤", "月晕", "星辉",
                "悲伤", "难过", "伤心", "低落", "沮丧", "失落", "失望", "想哭", "心酸", "沉重"),
            ["愤怒"] = new("愤怒", "灼华", "朱砂",
                "愤怒", "生气", "恼火", "火大", "气愤", "恼怒", "暴躁", "烦躁", "气死", "发火"),
            ["平静"] = new("平静", "凝霜", "垂露",
                "平静", "安静", "放松", "宁静", "平和", "淡定", "安宁", "稳定", "舒服"),
            ["爱"] = new("爱", "合欢", "渐暖",
                "爱", "喜欢", "心动", "亲近", "温柔", "依恋", "思念", "想你", "在乎", "珍惜"),
            ["恐惧"] = new("恐惧", "颤音", "悬心",
                "恐惧", "害怕", "担心", "紧张", "焦虑", "惊慌", "慌", "不安", "忐忑", "退缩"),
            ["惊讶"] = new("惊讶", "星爆", "惊风",
                "惊讶", "震惊", "意外", "吃惊", "诧异", "突然", "没想到", "讶异", "哇"),
            ["期待"] = new("期待", "顾盼", "含苞",
                "期待", "盼望", "希望", "等待", "憧憬", "想要", "盼着", "希冀", "期待中"),
            ["孤独"] = new("孤独", "疏影", "薄暮",
                "孤独", "孤单", "寂寞", "一个人", "独自", "落单", "冷清", "空落", "被丢下"),
        };

        public static string ClassifyEmotion(string emotionDetail)
        {
            string source = NormalizeForMatch(emotionDetail);
            if (string.IsNullOrWhiteSpace(source))
            {
                return DefaultEmotionType;
            }

            string bestEmotion = DefaultEmotionType;
            int bestScore = 0;

            foreach (string emotion in EmotionOrder)
            {
                if (!Definitions.TryGetValue(emotion, out var definition))
                {
                    continue;
                }

                int score = 0;
                foreach (string keyword in definition.Keywords)
                {
                    if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score++;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEmotion = definition.EmotionType;
                }
            }

            return bestScore > 0 ? bestEmotion : DefaultEmotionType;
        }

        public static bool IsKnownEmotionType(string emotionType)
        {
            string normalized = NormalizeForMatch(emotionType);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (Definitions.ContainsKey(normalized))
            {
                return true;
            }

            foreach (var definition in Definitions.Values)
            {
                foreach (string keyword in definition.Keywords)
                {
                    if (string.Equals(normalized, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string NormalizeEmotionType(string emotionType)
        {
            string normalized = NormalizeForMatch(emotionType);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return DefaultEmotionType;
            }

            if (Definitions.ContainsKey(normalized))
            {
                return normalized;
            }

            foreach (var definition in Definitions.Values)
            {
                foreach (string keyword in definition.Keywords)
                {
                    if (string.Equals(normalized, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return definition.EmotionType;
                    }
                }
            }

            return DefaultEmotionType;
        }

        public static string ResolveFlowerName(string emotionType, string owner)
        {
            string canonicalEmotion = NormalizeEmotionType(emotionType);
            if (!Definitions.TryGetValue(canonicalEmotion, out var definition))
            {
                return "未知花卉";
            }

            return NormalizeOwner(owner) == OwnerDemon
                ? definition.DemonFlowerName
                : definition.AngelFlowerName;
        }

        public static string ResolveEmotionDisplayName(string emotionType)
        {
            return NormalizeEmotionType(emotionType);
        }

        public static string NormalizeOwner(string owner)
        {
            if (string.Equals(owner, OwnerDemon, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(owner, "devil", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(owner, "evil", StringComparison.OrdinalIgnoreCase))
            {
                return OwnerDemon;
            }

            return OwnerAngel;
        }

        public static string ResolveOwnerDisplayName(string owner)
        {
            return NormalizeOwner(owner) == OwnerDemon ? "恶魔" : "天使";
        }

        public static int GetEmotionSortIndex(string emotionType)
        {
            string canonicalEmotion = NormalizeEmotionType(emotionType);
            for (int i = 0; i < EmotionOrder.Length; i++)
            {
                if (string.Equals(EmotionOrder[i], canonicalEmotion, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        public static int GetOwnerSortIndex(string owner)
        {
            return NormalizeOwner(owner) == OwnerDemon ? 1 : 0;
        }

        private static string NormalizeForMatch(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace(" ", string.Empty).Replace("　", string.Empty);
        }

        private sealed class EmotionDefinition
        {
            public EmotionDefinition(
                string emotionType,
                string angelFlowerName,
                string demonFlowerName,
                params string[] keywords)
            {
                EmotionType = emotionType;
                AngelFlowerName = angelFlowerName;
                DemonFlowerName = demonFlowerName;
                Keywords = keywords;
            }

            public string EmotionType { get; }
            public string AngelFlowerName { get; }
            public string DemonFlowerName { get; }
            public string[] Keywords { get; }
        }
    }
}
