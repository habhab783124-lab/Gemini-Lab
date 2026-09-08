#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 苹果资源默认实现。
    ///
    /// 每棵树独立记录下一轮现实时间生成点、当日已生成轮数和未领取缓存。
    /// 生成点和缓存均进入存档，因此退出、重启或离开 WorldMap 不会丢失成熟苹果。
    /// </summary>
    public sealed class AppleService : IAppleService
    {
        public const int DefaultInitialBalance = 20;
        public const int DefaultGenerationIntervalMinMinutes = 45;
        public const int DefaultGenerationIntervalMaxMinutes = 90;
        public const int DefaultMaxRoundsPerDay = 5;
        public const double GenerationOneAppleProbability = 0.70d;

        // 兼容旧代码/旧检查器的名称；新版使用 Min/Max 两个间隔。
        public const int DefaultGenerationIntervalMinutes = DefaultGenerationIntervalMinMinutes;
        public const int DefaultMaxPendingPerTree = int.MaxValue;

        private const int SaveVersion = 3;
        private const int MaxGenerationCatchUpIterations = 4096;

        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;
        private readonly Dictionary<string, AppleTreeState> _trees = new(StringComparer.Ordinal);
        private readonly System.Random _random;

        public AppleService(
            IGameClock clock,
            EventBus? eventBus,
            int initialBalance = DefaultInitialBalance,
            int generationIntervalMinutes = DefaultGenerationIntervalMinMinutes,
            int maxPendingPerTree = DefaultMaxPendingPerTree,
            int generationIntervalMaxMinutes = DefaultGenerationIntervalMaxMinutes,
            int maxRoundsPerDay = DefaultMaxRoundsPerDay,
            int? randomSeed = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            _random = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();

            InitialBalance = Mathf.Max(0, initialBalance);
            Balance = InitialBalance;
            GenerationIntervalMinMinutes = Mathf.Max(1, generationIntervalMinutes);
            GenerationIntervalMaxMinutes = Mathf.Max(GenerationIntervalMinMinutes, generationIntervalMaxMinutes);
            MaxRoundsPerDay = Mathf.Max(1, maxRoundsPerDay);
        }

        public string Key => "apple";
        public int Balance { get; private set; }
        public int InitialBalance { get; }

        // Kept for consumers that only need a representative interval.
        public int GenerationIntervalMinutes => GenerationIntervalMinMinutes;
        public int GenerationIntervalMinMinutes { get; }
        public int GenerationIntervalMaxMinutes { get; }
        public int MaxRoundsPerDay { get; }

        // The new requirement has no pending-cache cap. This property remains for API compatibility.
        public int MaxPendingPerTree => int.MaxValue;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Balance = SafeAdd(Balance, amount);
            _eventBus?.Publish(new AppleChangedEvent(Balance, amount));
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Balance < amount) return false;
            Balance -= amount;
            _eventBus?.Publish(new AppleChangedEvent(Balance, -amount));
            return true;
        }

        public void EnsureTree(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return;
            string normalized = treeId.Trim();
            if (_trees.ContainsKey(normalized)) return;

            DateTime now = _clock.UtcNow;
            var state = new AppleTreeState
            {
                TreeId = normalized,
                LastGeneratedUtcTicks = now.Ticks,
                NextGenerationUtcTicks = SafeAddTicks(now.Ticks, NextIntervalTicks()),
                GenerationDayKey = DayKey(now),
                GeneratedRoundsToday = 0,
                PendingCount = 0,
                TotalCollected = 0
            };
            _trees.Add(normalized, state);
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
        }

        public int GetPendingCount(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return 0;
            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];
            GeneratePending(ref state);
            _trees[normalized] = state;
            return state.PendingCount;
        }

        /// <summary>
        /// Reserves the current cached amount for one visible tree-shake
        /// interaction. The amount is not added to the balance until the
        /// authored ground apples are collected one by one.
        /// </summary>
        public bool TryBeginHarvest(string treeId, out int total)
        {
            total = 0;
            if (string.IsNullOrWhiteSpace(treeId)) return false;

            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];
            GeneratePending(ref state);

            if (state.HarvestRemaining > 0)
            {
                total = state.HarvestRemaining;
                _trees[normalized] = state;
                return true;
            }

            if (state.PendingCount <= 0)
            {
                _trees[normalized] = state;
                _eventBus?.Publish(new AppleTreeChangedEvent(state));
                return false;
            }

            total = state.PendingCount;
            state.PendingCount = 0;
            state.HarvestRemaining = total;
            state.HarvestTotal = total;
            _trees[normalized] = state;
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
            return true;
        }

        /// <summary>
        /// Collects at most one authored drop from the active harvest session.
        /// The service is the single authority for the fixed batch total.
        /// </summary>
        public bool TryCollectHarvest(string treeId, int amount)
        {
            if (string.IsNullOrWhiteSpace(treeId) || amount <= 0) return false;

            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];
            int collected = Mathf.Min(amount, state.HarvestRemaining);
            if (collected <= 0) return false;

            state.HarvestRemaining -= collected;
            state.TotalCollected = SafeAdd(state.TotalCollected, collected);
            if (state.HarvestRemaining == 0)
            {
                state.HarvestTotal = 0;
            }

            _trees[normalized] = state;
            Add(collected);
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
            return true;
        }

        public int GetHarvestRemaining(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return 0;
            EnsureTree(treeId);
            string normalized = treeId.Trim();
            return _trees[normalized].HarvestRemaining;
        }

        public int ShakeTree(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return 0;
            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];

            // Keep the legacy API safe for existing callers: if a new
            // incremental harvest is already active, collect its remainder
            // as one legacy operation instead of starting a second batch.
            if (state.HarvestRemaining > 0)
            {
                int active = state.HarvestRemaining;
                state.HarvestRemaining = 0;
                state.HarvestTotal = 0;
                state.TotalCollected = SafeAdd(state.TotalCollected, active);
                _trees[normalized] = state;
                Add(active);
                _eventBus?.Publish(new AppleTreeChangedEvent(state));
                _eventBus?.Publish(new AppleTreeShakenEvent(normalized, active));
                return active;
            }

            GeneratePending(ref state);

            int collected = state.PendingCount;
            if (collected <= 0)
            {
                _trees[normalized] = state;
                _eventBus?.Publish(new AppleTreeChangedEvent(state));
                return 0;
            }

            state.PendingCount = 0;
            state.TotalCollected = SafeAdd(state.TotalCollected, collected);
            _trees[normalized] = state;
            Add(collected);
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
            _eventBus?.Publish(new AppleTreeShakenEvent(normalized, collected));
            return collected;
        }

        public IReadOnlyList<AppleTreeState> GetTreeStates()
        {
            var result = new List<AppleTreeState>(_trees.Count);
            foreach (string treeId in new List<string>(_trees.Keys))
            {
                GetPendingCount(treeId);
                result.Add(_trees[treeId]);
            }
            return result;
        }

        public string CaptureJson()
        {
            var states = new List<AppleTreeState>(_trees.Count);
            foreach (var state in _trees.Values)
            {
                states.Add(state);
            }

            return JsonUtility.ToJson(new SavePayload
            {
                Version = SaveVersion,
                Balance = Mathf.Max(0, Balance),
                Trees = states
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                if (payload == null) return false;

                Balance = Mathf.Max(0, payload.Balance);
                _trees.Clear();
                if (payload.Trees != null)
                {
                    foreach (var saved in payload.Trees)
                    {
                        if (string.IsNullOrWhiteSpace(saved.TreeId)) continue;
                        var state = saved;
                        state.TreeId = state.TreeId.Trim();
                        NormalizeRestoredState(ref state, payload.Version);
                        _trees[state.TreeId] = state;
                    }
                }

                _eventBus?.Publish(new AppleChangedEvent(Balance, 0));
                foreach (var state in _trees.Values)
                {
                    _eventBus?.Publish(new AppleTreeChangedEvent(state));
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AppleService] 存档恢复失败：{e.Message}");
                return false;
            }
        }

        private void GeneratePending(ref AppleTreeState state)
        {
            DateTime now = _clock.UtcNow;
            EnsureGenerationSchedule(ref state, now);

            // A development clock can be advanced and later reset. In that
            // case an empty tree may contain a schedule entirely in the
            // future relative to the current clock and would otherwise stay
            // blocked until the real clock catches up. Do not touch a
            // pending or active harvest: those are player-owned progress.
            if (state.PendingCount <= 0 &&
                state.HarvestRemaining <= 0 &&
                state.LastGeneratedUtcTicks > now.Ticks)
            {
                state.GenerationDayKey = DayKey(now);
                state.GeneratedRoundsToday = 0;
                state.LastGeneratedUtcTicks = now.Ticks;
                state.NextGenerationUtcTicks = now.Ticks;
                Debug.LogWarning(
                    $"[AppleService][ClockRollback] tree={state.TreeId} " +
                    $"scheduleReanchored={now:O}");
            }

            // A debug day advance is expected to make a tree usable on the new
            // calendar day. If the previous day ended with no cached or active
            // harvest, make the first round of the new day due immediately;
            // normal 45-90 minute scheduling continues after that round.
            if (state.PendingCount <= 0 &&
                state.HarvestRemaining <= 0 &&
                state.LastGeneratedUtcTicks > 0 &&
                new DateTime(state.LastGeneratedUtcTicks, DateTimeKind.Utc).Date < now.Date)
            {
                state.GenerationDayKey = DayKey(now);
                state.GeneratedRoundsToday = 0;
                state.NextGenerationUtcTicks = now.Ticks;
                Debug.Log($"[AppleService][DayBoundary] tree={state.TreeId} dueNow={now:O}");
            }

            long nextTicks = state.NextGenerationUtcTicks;
            int iterations = 0;
            while (nextTicks > 0 && nextTicks <= now.Ticks && iterations++ < MaxGenerationCatchUpIterations)
            {
                DateTime dueAt = new DateTime(nextTicks, DateTimeKind.Utc);
                string dueDay = DayKey(dueAt);
                if (!string.Equals(state.GenerationDayKey, dueDay, StringComparison.Ordinal))
                {
                    state.GenerationDayKey = dueDay;
                    state.GeneratedRoundsToday = 0;
                }

                if (state.GeneratedRoundsToday >= MaxRoundsPerDay)
                {
                    DateTime nextDay = dueAt.Date.AddDays(1);
                    nextTicks = SafeAddTicks(nextDay.Ticks, NextIntervalTicks());
                    state.NextGenerationUtcTicks = nextTicks;
                    continue;
                }

                int quantity = _random.NextDouble() < GenerationOneAppleProbability ? 1 : 2;
                state.PendingCount = SafeAdd(state.PendingCount, quantity);
                state.GeneratedRoundsToday++;
                state.LastGeneratedUtcTicks = dueAt.Ticks;
                nextTicks = SafeAddTicks(nextTicks, NextIntervalTicks());
                state.NextGenerationUtcTicks = nextTicks;
            }
        }

        private void EnsureGenerationSchedule(ref AppleTreeState state, DateTime now)
        {
            if (state.LastGeneratedUtcTicks <= 0)
            {
                state.LastGeneratedUtcTicks = now.Ticks;
            }

            if (string.IsNullOrWhiteSpace(state.GenerationDayKey))
            {
                state.GenerationDayKey = DayKey(
                    state.NextGenerationUtcTicks > 0
                        ? new DateTime(state.NextGenerationUtcTicks, DateTimeKind.Utc)
                        : now);
            }

            if (state.NextGenerationUtcTicks <= 0)
            {
                state.NextGenerationUtcTicks = SafeAddTicks(now.Ticks, NextIntervalTicks());
            }
        }

        private void NormalizeRestoredState(ref AppleTreeState state, int payloadVersion)
        {
            DateTime now = _clock.UtcNow;
            state.LastGeneratedUtcTicks = state.LastGeneratedUtcTicks > 0
                ? state.LastGeneratedUtcTicks
                : now.Ticks;
            state.PendingCount = Mathf.Max(0, state.PendingCount);
            state.HarvestRemaining = Mathf.Max(0, state.HarvestRemaining);
            state.HarvestTotal = Mathf.Max(state.HarvestRemaining, state.HarvestTotal);
            state.TotalCollected = Mathf.Max(0, state.TotalCollected);
            state.GeneratedRoundsToday = Mathf.Clamp(state.GeneratedRoundsToday, 0, MaxRoundsPerDay);

            // Version 1 only stored LastGeneratedUtcTicks. Give it a schedule without
            // resetting the existing pending cache, so old saves migrate once.
            if (payloadVersion < SaveVersion || state.NextGenerationUtcTicks <= 0)
            {
                state.NextGenerationUtcTicks = SafeAddTicks(state.LastGeneratedUtcTicks, NextIntervalTicks());
            }

            if (string.IsNullOrWhiteSpace(state.GenerationDayKey))
            {
                state.GenerationDayKey = DayKey(
                    new DateTime(state.NextGenerationUtcTicks, DateTimeKind.Utc));
            }
        }

        private long NextIntervalTicks()
        {
            int minutes = _random.Next(GenerationIntervalMinMinutes, GenerationIntervalMaxMinutes + 1);
            return TimeSpan.FromMinutes(minutes).Ticks;
        }

        private static string DayKey(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int SafeAdd(int left, int right)
        {
            long sum = (long)left + right;
            return sum >= int.MaxValue ? int.MaxValue : (int)Math.Max(0, sum);
        }

        private static long SafeAddTicks(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
            return left + right;
        }

        [Serializable]
        private sealed class SavePayload
        {
            public int Version;
            public int Balance;
            public List<AppleTreeState> Trees = new();
        }
    }
}
