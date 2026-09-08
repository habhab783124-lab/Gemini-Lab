#nullable enable
using System;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 让 Pet 的两只运行态数据随 SaveBundle 走。
    /// 实现 <see cref="IPersistentService"/>，由 PetRuntimeBootstrap 注册。
    ///
    /// Payload v2（数值规则文档 §20 离线规则）：
    /// - 附带保存时刻 <c>savedAtUtcTicks</c>
    /// - 恢复时按离线时长让心情向 50 轻量回归：每离线 5 分钟回归 1 点（与在线 §13 同节奏），
    ///   最多修正 6 点（即最多模拟 30 分钟）；不越过 50
    /// - 精力离线不衰减，原样恢复
    /// - v1 存档（无 savedAtUtcTicks）兼容恢复，不做离线回归
    /// </summary>
    public sealed class PetRuntimeSaveService : IPersistentService
    {
        /// <summary>离线心情回归速率（秒/点），与在线 §13 的 5 分钟/点一致。</summary>
        public const float OfflineMoodRegressionSecondsPerPoint = 300f;

        /// <summary>离线心情回归最大修正点数（§20：最多模拟 30 分钟）。</summary>
        public const int OfflineMoodRegressionMaxPoints = 6;

        private readonly IPetRoster _roster;
        private readonly Func<DateTime> _utcNow;

        public PetRuntimeSaveService(IPetRoster roster, Func<DateTime>? utcNow = null)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public string Key => "pet_runtime";

        [Serializable]
        private struct Entry
        {
            public int petId;
            public float mood;
            public float energy;
            public float satiety;
            public float relation;
            public float runtimeTime;
            public int travelCompletedCount;
            public string currentState;
            public string lastInteractionFurnitureId;
            public string lastInteractionSummary;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public Entry[] entries;
            public long savedAtUtcTicks; // v2 新增；v1 存档中为 0
        }

        public string CaptureJson()
        {
            var pets = _roster.RegisteredPets;
            var entries = new Entry[pets.Count];
            for (int i = 0; i < pets.Count; i++)
            {
                var id = pets[i];
                var d = _roster.TryGet(id);
                if (d == null)
                {
                    entries[i] = new Entry { petId = (int)id };
                    continue;
                }
                entries[i] = new Entry
                {
                    petId = (int)id,
                    mood = d.Mood,
                    energy = d.Energy,
                    satiety = d.Satiety,
                    relation = d.Relation,
                    runtimeTime = d.RuntimeTimeSeconds,
                    travelCompletedCount = d.TravelCompletedCount,
                    currentState = d.CurrentState,
                    lastInteractionFurnitureId = d.LastInteractionFurnitureId ?? string.Empty,
                    lastInteractionSummary = d.LastInteractionSummary ?? string.Empty
                };
            }
            return JsonUtility.ToJson(new SavePayload
            {
                version = 2,
                entries = entries,
                savedAtUtcTicks = _utcNow().Ticks
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                if (payload.entries == null) return true;

                // §20：v2 起按离线时长计算心情回归点数；v1 存档无时间戳，不回归。
                int regressionPoints = 0;
                if (payload.version >= 2 && payload.savedAtUtcTicks > 0L)
                {
                    var savedAtUtc = new DateTime(payload.savedAtUtcTicks, DateTimeKind.Utc);
                    double offlineSeconds = Math.Max(0.0, (_utcNow() - savedAtUtc).TotalSeconds);
                    regressionPoints = Math.Min(
                        OfflineMoodRegressionMaxPoints,
                        (int)(offlineSeconds / OfflineMoodRegressionSecondsPerPoint));
                }

                foreach (var e in payload.entries)
                {
                    var petId = (PetId)e.petId;
                    var d = _roster.TryGet(petId);
                    if (d == null) continue;
                    d.Mood = ApplyOfflineMoodRegression(e.mood, regressionPoints);
                    d.Energy = e.energy; // §20：离线不进行自然精力衰减，原样恢复。
                    d.Satiety = e.satiety;
                    // v1 没有 relation 字段。缺字段时 JsonUtility 会给 0，
                    // 因此只有 v2+ 才覆盖当前默认值，避免旧存档把亲密度意外清零。
                    if (payload.version >= 2)
                    {
                        d.Relation = e.relation;
                    }
                    d.RuntimeTimeSeconds = e.runtimeTime;
                    d.TravelCompletedCount = e.travelCompletedCount;
                    d.CurrentState = e.currentState ?? "None";
                    d.LastInteractionFurnitureId = e.lastInteractionFurnitureId ?? string.Empty;
                    d.LastInteractionSummary = e.lastInteractionSummary ?? string.Empty;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>让心情向 50 回归 <paramref name="points"/> 点，不越过 50。</summary>
        private static float ApplyOfflineMoodRegression(float mood, int points)
        {
            if (points <= 0) return mood;
            const float neutral = 50f;
            return mood > neutral
                ? Mathf.Max(neutral, mood - points)
                : Mathf.Min(neutral, mood + points);
        }
    }
}
