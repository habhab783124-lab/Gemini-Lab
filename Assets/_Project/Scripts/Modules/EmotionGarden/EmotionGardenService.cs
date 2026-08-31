#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Apple;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GeminiLab.Modules.EmotionGarden
{
    /// <summary>
    /// 情绪花园核心服务。管理每日情绪提交、周记录、开花状态和图鉴累计。
    /// 存档 key: "emotion-garden"。
    /// </summary>
    public sealed class EmotionGardenService : MonoBehaviour, IEmotionGardenService, IPersistentService
    {
        private const int SaveVersion = 4;
        private const int PlacementInventorySaveVersion = 3;
        private const int PlacedFlowersSaveVersion = 4;

        private IGameClock? _clock;
        private EventBus? _eventBus;

        private string _lastSubmitDateIso = string.Empty;
        private readonly List<EmotionFlowerData> _flowers = new();
        private readonly Dictionary<string, ClusterProgress> _clusters = new(); // key: "emotionType|owner"
        private readonly Dictionary<string, PlacementFlowerInventory> _placementInventories = new();
        private readonly List<PlacedEmotionFlower> _placedFlowers = new();
        private readonly List<EmotionDailySummaryData> _dailySummaries = new();

        string IPersistentService.Key => "emotion-garden";

        public void Initialize(IGameClock clock, EventBus eventBus)
        {
            _clock = clock;
            _eventBus = eventBus;
        }

        // ── IEmotionGardenService ──────────────────────────────

        public bool CanSubmitToday()
        {
            if (_clock == null) return false;
            return _lastSubmitDateIso != _clock.TodayIso;
        }

        public EmotionFlowerData? SubmitEmotion(string emotionType, string emotionDetail, string owner)
        {
            if (_clock == null) return null;
            if (!CanSubmitToday()) return null;

            var resolvedEmotionType = EmotionFlowerCatalog.IsKnownEmotionType(emotionType)
                ? EmotionFlowerCatalog.NormalizeEmotionType(emotionType)
                : EmotionFlowerCatalog.ClassifyEmotion(emotionDetail);
            var normalizedOwner = EmotionFlowerCatalog.NormalizeOwner(owner);
            var flowerName = EmotionFlowerCatalog.ResolveFlowerName(resolvedEmotionType, normalizedOwner);

            var today = _clock.TodayIso;
            var weekId = ComputeWeekIdFromIso(today);

            var flower = new EmotionFlowerData
            {
                FlowerId = $"{resolvedEmotionType}_{normalizedOwner}_{today}",
                DateIso = today,
                WeekId = weekId,
                EmotionType = resolvedEmotionType,
                FlowerName = flowerName,
                EmotionDetail = emotionDetail,
                Owner = normalizedOwner,
                State = GrowthState.Growing,
                IsCollected = false,
                CreatedAtUtcTicks = _clock.UtcNow.Ticks
            };

            _lastSubmitDateIso = today;
            _flowers.Add(flower);

            UpsertDailySummary(BuildDailySummary(flower));

            _eventBus?.Publish(new EmotionFlowerSubmittedEvent(flower));

            Debug.Log($"[EmotionGarden] 提交情绪: {flower.FlowerName} ({flower.EmotionType}/{flower.Owner})");
            return flower;
        }

        public EmotionFlowerData? GetTodayFlower()
        {
            if (_clock == null) return null;
            var today = _clock.TodayIso;
            for (int i = _flowers.Count - 1; i >= 0; i--)
            {
                if (_flowers[i].DateIso == today) return _flowers[i];
            }
            return null;
        }

        public EmotionDailySummaryData? GetDailySummary(string dateIso)
        {
            if (string.IsNullOrWhiteSpace(dateIso)) return null;

            for (int i = _dailySummaries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_dailySummaries[i].DateIso, dateIso, StringComparison.Ordinal))
                {
                    return _dailySummaries[i];
                }
            }

            return null;
        }

        public IReadOnlyList<string> GetDailySummaryDates()
        {
            var dates = new List<string>(_dailySummaries.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var summary in _dailySummaries)
            {
                if (!string.IsNullOrWhiteSpace(summary.DateIso) && seen.Add(summary.DateIso))
                {
                    dates.Add(summary.DateIso);
                }
            }

            dates.Sort(StringComparer.Ordinal);
            dates.Reverse();
            return dates;
        }

        public EmotionDailySummaryData? GetTodayDailySummary()
        {
            return _clock == null ? null : GetDailySummary(_clock.TodayIso);
        }

        public int GetCurrentWeekId()
        {
            if (_clock == null) return 0;
            return ComputeWeekIdFromIso(_clock.TodayIso);
        }

        public int OffsetWeekId(int weekId, int deltaWeeks)
        {
            var monday = GetWeekStartDate(weekId);
            return ComputeWeekId(monday.AddDays(deltaWeeks * 7));
        }

        public DateTime GetWeekStartDate(int weekId)
        {
            int year = weekId / 100;
            int week = weekId % 100;
            if (year < 1 || week < 1) return DateTime.MinValue;

            // ISO 8601: 1 月 4 日必属第 1 周
            var jan4 = new DateTime(year, 1, 4);
            int dow = ((int)jan4.DayOfWeek + 6) % 7; // 周一=0 … 周日=6
            var week1Monday = jan4.AddDays(-dow);
            return week1Monday.AddDays((week - 1) * 7);
        }

        public EmotionFlowerData?[] GetWeekFlowers(int weekId)
        {
            var result = new EmotionFlowerData?[7];

            for (int i = 0; i < _flowers.Count; i++)
            {
                var f = _flowers[i];
                if (f.WeekId != weekId) continue;

                if (DateTime.TryParseExact(f.DateIso, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    int dayIndex = ((int)dt.DayOfWeek + 6) % 7; // 周一=0 … 周日=6
                    result[dayIndex] = f;
                }
            }

            return result;
        }

        public bool SetBloomed(string flowerId)
        {
            for (int i = 0; i < _flowers.Count; i++)
            {
                if (_flowers[i].FlowerId != flowerId) continue;
                return BloomAt(i);
            }

            return false;
        }

        /// <summary>按索引开花。调试重置后同日重复提交会产生重复 FlowerId，按 ID 查找会漏开。</summary>
        private bool BloomAt(int i)
        {
            var f = _flowers[i];
            if (f.State == GrowthState.Bloomed && f.IsCollected) return false;

            f.State = GrowthState.Bloomed;
            f.IsCollected = true;
            _flowers[i] = f;

            // 更新累计进度
            var cluster = GetOrCreateCluster(f.EmotionType, f.Owner);
            cluster.TotalCount += 1;

            // 解锁阶段判定
            int newStage = cluster.UnlockedStage;
            if (cluster.TotalCount >= 3) newStage = 3;
            else if (cluster.TotalCount >= 1) newStage = 1;

            if (newStage != cluster.UnlockedStage)
            {
                cluster.UnlockedStage = newStage;
                _eventBus?.Publish(new ClusterUnlockedEvent(f.EmotionType, f.Owner, newStage));
            }

            _clusters[ClusterKey(f.EmotionType, f.Owner)] = cluster;
            var inventory = GetOrCreatePlacementInventory(f.EmotionType, f.Owner);
            inventory.SingleCount += 1;
            _placementInventories[PlacementInventoryKey(f.EmotionType, f.Owner)] = inventory;
            if (ServiceLocator.TryResolve(out IAppleService? apples) && apples is not null)
            {
                apples.Add(12);
            }
            _eventBus?.Publish(new EmotionFlowerBloomedEvent(f.FlowerId));
            PublishPlacementInventoryChanged(inventory);

            Debug.Log($"[EmotionGarden] 开花: {f.FlowerId}, 累计: {cluster.TotalCount}, 阶段: {cluster.UnlockedStage}");
            return true;
        }

        public IReadOnlyList<ClusterProgress> GetAllClusters()
        {
            var list = new List<ClusterProgress>(_clusters.Values);
            return list;
        }

        public PlacementFlowerInventory GetPlacementInventory(string emotionType, string owner)
        {
            string normalizedEmotion = EmotionFlowerCatalog.NormalizeEmotionType(emotionType);
            string normalizedOwner = EmotionFlowerCatalog.NormalizeOwner(owner);
            string key = PlacementInventoryKey(normalizedEmotion, normalizedOwner);
            return _placementInventories.TryGetValue(key, out var inventory)
                ? inventory
                : new PlacementFlowerInventory
                {
                    EmotionType = normalizedEmotion,
                    Owner = normalizedOwner
                };
        }

        public bool TryConsumePlacementSingle(string emotionType, string owner, int amount = 1)
        {
            if (amount <= 0) return false;

            var inventory = GetPlacementInventory(emotionType, owner);
            if (inventory.SingleCount < amount) return false;

            inventory.SingleCount -= amount;
            _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            PublishPlacementInventoryChanged(inventory);
            return true;
        }

        public bool TryConsumePlacementCluster(string emotionType, string owner, int amount = 1)
        {
            if (amount <= 0) return false;

            var inventory = GetPlacementInventory(emotionType, owner);
            if (inventory.ClusterCount < amount) return false;

            inventory.ClusterCount -= amount;
            _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            PublishPlacementInventoryChanged(inventory);
            return true;
        }

        public bool TrySynthesizePlacementCluster(string emotionType, string owner)
        {
            var inventory = GetPlacementInventory(emotionType, owner);
            if (inventory.SingleCount < 3) return false;

            inventory.SingleCount -= 3;
            inventory.ClusterCount += 1;
            _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            PublishPlacementInventoryChanged(inventory);
            return true;
        }

        public IReadOnlyList<PlacedEmotionFlower> GetPlacedFlowers()
        {
            return new List<PlacedEmotionFlower>(_placedFlowers);
        }

        public bool TryPlaceFlower(
            string emotionType,
            string owner,
            bool isCluster,
            int slotIndex,
            float worldX,
            float worldY,
            string placementLayerId = "")
        {
            if (slotIndex < 0 || float.IsNaN(worldX) || float.IsInfinity(worldX) ||
                float.IsNaN(worldY) || float.IsInfinity(worldY))
            {
                return false;
            }

            for (int i = 0; i < _placedFlowers.Count; i++)
            {
                if (_placedFlowers[i].SlotIndex == slotIndex)
                {
                    return false;
                }
            }

            var inventory = GetPlacementInventory(emotionType, owner);
            if (isCluster)
            {
                if (inventory.ClusterCount < 1) return false;
                inventory.ClusterCount -= 1;
            }
            else
            {
                if (inventory.SingleCount < 1) return false;
                inventory.SingleCount -= 1;
            }

            var placed = new PlacedEmotionFlower
            {
                SlotIndex = slotIndex,
                EmotionType = inventory.EmotionType,
                Owner = inventory.Owner,
                IsCluster = isCluster,
                WorldX = worldX,
                WorldY = worldY,
                PlacementLayerId = placementLayerId ?? string.Empty
            };

            _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            _placedFlowers.Add(placed);
            PublishPlacementInventoryChanged(inventory);
            _eventBus?.Publish(new EmotionFlowerPlacementsChangedEvent());
            return true;
        }

        public void RefreshBlooming()
        {
            if (_clock == null) return;
            var today = _clock.TodayIso;

            for (int i = 0; i < _flowers.Count; i++)
            {
                var f = _flowers[i];
                if (f.State != GrowthState.Growing) continue;
                if (f.IsCollected) continue;

                // 跨天自动开花：花的日期早于今天
                if (string.Compare(f.DateIso, today, StringComparison.Ordinal) < 0)
                {
                    BloomAt(i);
                }
            }
        }

        public void ClearAllData()
        {
            _flowers.Clear();
            _clusters.Clear();
            _placementInventories.Clear();
            _placedFlowers.Clear();
            _dailySummaries.Clear();
            _lastSubmitDateIso = string.Empty;
            _eventBus?.Publish(new EmotionFlowerPlacementInventoryChangedEvent(default));
            _eventBus?.Publish(new EmotionFlowerPlacementsChangedEvent());
            _eventBus?.Publish(new EmotionGardenClearedEvent());
            Debug.Log("[EmotionGarden] 花园数据已清空（调试）");
        }

        // ── IPersistentService ─────────────────────────────────

        string IPersistentService.CaptureJson()
        {
            var save = new EmotionGardenSaveData
            {
                Version = SaveVersion,
                LastSubmitDateIso = _lastSubmitDateIso,
                Flowers = new List<EmotionFlowerData>(_flowers),
                Clusters = new List<ClusterProgress>(_clusters.Values),
                PlacementInventories = new List<PlacementFlowerInventory>(_placementInventories.Values),
                PlacedFlowers = new List<PlacedEmotionFlower>(_placedFlowers),
                DailySummaries = new List<EmotionDailySummaryData>(_dailySummaries)
            };
            return JsonUtility.ToJson(save);
        }

        bool IPersistentService.RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var save = JsonUtility.FromJson<EmotionGardenSaveData>(json);
                if (save == null) return false;

                _lastSubmitDateIso = save.LastSubmitDateIso ?? string.Empty;
                _flowers.Clear();
                if (save.Flowers != null) _flowers.AddRange(save.Flowers);

                // 迁移：WeekId 从 DateIso 重算（旧存档存的是不含年份的裸周号，且可能有 UTC 漂移）
                for (int i = 0; i < _flowers.Count; i++)
                {
                    var f = _flowers[i];
                    f.Owner = EmotionFlowerCatalog.NormalizeOwner(f.Owner);
                    f.EmotionType = EmotionFlowerCatalog.IsKnownEmotionType(f.EmotionType)
                        ? EmotionFlowerCatalog.NormalizeEmotionType(f.EmotionType)
                        : EmotionFlowerCatalog.DefaultEmotionType;
                    if (string.IsNullOrWhiteSpace(f.FlowerName))
                    {
                        f.FlowerName = EmotionFlowerCatalog.ResolveFlowerName(f.EmotionType, f.Owner);
                    }

                    var recomputed = ComputeWeekIdFromIso(f.DateIso);
                    if (recomputed != 0 && recomputed != f.WeekId)
                    {
                        f.WeekId = recomputed;
                    }

                    _flowers[i] = f;
                }

                _clusters.Clear();
                if (save.Clusters != null)
                {
                    foreach (var c in save.Clusters)
                    {
                        var normalized = c;
                        normalized.Owner = EmotionFlowerCatalog.NormalizeOwner(normalized.Owner);
                        normalized.EmotionType = EmotionFlowerCatalog.IsKnownEmotionType(normalized.EmotionType)
                            ? EmotionFlowerCatalog.NormalizeEmotionType(normalized.EmotionType)
                            : EmotionFlowerCatalog.DefaultEmotionType;
                        _clusters[ClusterKey(normalized.EmotionType, normalized.Owner)] = normalized;
                    }
                }

                _placementInventories.Clear();
                if (save.Version >= PlacementInventorySaveVersion && save.PlacementInventories != null)
                {
                    foreach (var savedInventory in save.PlacementInventories)
                    {
                        var normalized = NormalizePlacementInventory(savedInventory);
                        _placementInventories[PlacementInventoryKey(normalized.EmotionType, normalized.Owner)] = normalized;
                    }
                }
                else
                {
                    MigratePlacementInventoriesFromBloomedFlowers();
                }

                _placedFlowers.Clear();
                if (save.Version >= PlacedFlowersSaveVersion && save.PlacedFlowers != null)
                {
                    var occupiedSlots = new HashSet<int>();
                    foreach (var savedPlacement in save.PlacedFlowers)
                    {
                        if (!TryNormalizePlacedFlower(savedPlacement, occupiedSlots, out var normalized))
                        {
                            continue;
                        }

                        _placedFlowers.Add(normalized);
                        occupiedSlots.Add(normalized.SlotIndex);
                    }
                }

                _dailySummaries.Clear();
                if (save.DailySummaries != null)
                {
                    var seenDates = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var summary in save.DailySummaries)
                    {
                        if (string.IsNullOrWhiteSpace(summary.DateIso) || !seenDates.Add(summary.DateIso))
                        {
                            continue;
                        }

                        _dailySummaries.Add(summary);
                    }
                }

                // 兼容在每日小结功能加入前已经存在的情绪花：下次打开邮箱时也能看到可读的小结。
                for (int i = 0; i < _flowers.Count; i++)
                {
                    EmotionFlowerData flower = _flowers[i];
                    if (string.IsNullOrWhiteSpace(flower.DateIso) || GetDailySummary(flower.DateIso).HasValue)
                    {
                        continue;
                    }

                    UpsertDailySummary(BuildDailySummary(flower));
                }

                _eventBus?.Publish(new EmotionFlowerPlacementInventoryChangedEvent(default));
                _eventBus?.Publish(new EmotionFlowerPlacementsChangedEvent());

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EmotionGarden] 存档恢复失败: {e.Message}");
                return false;
            }
        }

        // ── Internal helpers ───────────────────────────────────

        /// <summary>
        /// ISO 8601 周编号，年份限定格式：年份*100+周号（如 202629）。
        /// 周四规则：一周的周四落在哪一年，该周就属于哪一年——
        /// 避免 .NET GetWeekOfYear 年末返回 53 而 ISO 应为次年第 1 周的缺陷，且不依赖系统区域设置。
        /// </summary>
        private static int ComputeWeekId(DateTime localDate)
        {
            int dow = ((int)localDate.DayOfWeek + 6) % 7; // 周一=0 … 周日=6
            var thursday = localDate.AddDays(3 - dow);
            int week = (thursday.DayOfYear - 1) / 7 + 1;
            return thursday.Year * 100 + week;
        }

        /// <summary>从 yyyy-MM-dd 推导周编号；解析失败返回 0。WeekId 的唯一事实来源是 DateIso。</summary>
        private static int ComputeWeekIdFromIso(string dateIso)
        {
            if (DateTime.TryParseExact(dateIso, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return ComputeWeekId(dt);
            }
            return 0;
        }

        private ClusterProgress GetOrCreateCluster(string emotionType, string owner)
        {
            var key = ClusterKey(emotionType, owner);
            if (_clusters.TryGetValue(key, out var existing)) return existing;
            return new ClusterProgress { EmotionType = emotionType, Owner = owner };
        }

        private PlacementFlowerInventory GetOrCreatePlacementInventory(string emotionType, string owner)
        {
            string normalizedEmotion = EmotionFlowerCatalog.NormalizeEmotionType(emotionType);
            string normalizedOwner = EmotionFlowerCatalog.NormalizeOwner(owner);
            string key = PlacementInventoryKey(normalizedEmotion, normalizedOwner);
            if (_placementInventories.TryGetValue(key, out var existing)) return existing;
            return new PlacementFlowerInventory
            {
                EmotionType = normalizedEmotion,
                Owner = normalizedOwner
            };
        }

        private void MigratePlacementInventoriesFromBloomedFlowers()
        {
            foreach (var flower in _flowers)
            {
                if (flower.State != GrowthState.Bloomed || !flower.IsCollected) continue;

                var inventory = GetOrCreatePlacementInventory(flower.EmotionType, flower.Owner);
                inventory.SingleCount += 1;
                _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            }

            // 旧存档可能没有保留完整的花记录；以累计进度补齐这类组合。
            foreach (var cluster in _clusters.Values)
            {
                var inventory = GetOrCreatePlacementInventory(cluster.EmotionType, cluster.Owner);
                inventory.SingleCount = Math.Max(inventory.SingleCount, cluster.TotalCount);
                _placementInventories[PlacementInventoryKey(inventory.EmotionType, inventory.Owner)] = inventory;
            }
        }

        private static PlacementFlowerInventory NormalizePlacementInventory(PlacementFlowerInventory inventory)
        {
            inventory.EmotionType = EmotionFlowerCatalog.NormalizeEmotionType(inventory.EmotionType);
            inventory.Owner = EmotionFlowerCatalog.NormalizeOwner(inventory.Owner);
            inventory.SingleCount = Math.Max(0, inventory.SingleCount);
            inventory.ClusterCount = Math.Max(0, inventory.ClusterCount);
            return inventory;
        }

        private static bool TryNormalizePlacedFlower(
            PlacedEmotionFlower placed,
            HashSet<int> occupiedSlots,
            out PlacedEmotionFlower normalized)
        {
            normalized = default;
            if (placed.SlotIndex < 0 || occupiedSlots.Contains(placed.SlotIndex) ||
                float.IsNaN(placed.WorldX) || float.IsInfinity(placed.WorldX) ||
                float.IsNaN(placed.WorldY) || float.IsInfinity(placed.WorldY))
            {
                return false;
            }

            placed.EmotionType = EmotionFlowerCatalog.NormalizeEmotionType(placed.EmotionType);
            placed.Owner = EmotionFlowerCatalog.NormalizeOwner(placed.Owner);
            placed.PlacementLayerId ??= string.Empty;
            normalized = placed;
            return true;
        }

        private void PublishPlacementInventoryChanged(PlacementFlowerInventory inventory)
        {
            _eventBus?.Publish(new EmotionFlowerPlacementInventoryChangedEvent(inventory));
        }

        private void UpsertDailySummary(EmotionDailySummaryData summary)
        {
            for (int i = 0; i < _dailySummaries.Count; i++)
            {
                if (!string.Equals(_dailySummaries[i].DateIso, summary.DateIso, StringComparison.Ordinal))
                {
                    continue;
                }

                _dailySummaries[i] = summary;
                return;
            }

            _dailySummaries.Add(summary);
        }

        private EmotionDailySummaryData BuildDailySummary(EmotionFlowerData flower)
        {
            string input = string.IsNullOrWhiteSpace(flower.EmotionDetail)
                ? "今天还没有写下具体心情。"
                : flower.EmotionDetail.Trim();
            string emotion = EmotionFlowerCatalog.ResolveEmotionDisplayName(flower.EmotionType);
            string flowerName = string.IsNullOrWhiteSpace(flower.FlowerName)
                ? EmotionFlowerCatalog.ResolveFlowerName(flower.EmotionType, flower.Owner)
                : flower.FlowerName;
            string description = $"这是一朵记录{emotion}的{flowerName}，把今天的心情留在花园里。";

            return new EmotionDailySummaryData
            {
                DateIso = flower.DateIso,
                InputSentence = TrimSummary(input, 120),
                EmotionType = emotion,
                FlowerName = flowerName,
                FlowerDescription = TrimSummary(description, 80),
                Summary = TrimSummary($"今天的{emotion}从“{input}”开始，最后在{flowerName}里留下了温柔的回声。", 60),
                AngelNote = TrimSummary($"天使日记：我看见你认真照顾自己的情绪。{flowerName}替你收好这份心意，明天也可以慢慢来。", 80),
                DevilNote = TrimSummary($"恶魔日记：别急着给今天下结论。你已经把“{input}”说出来了，这就足够让{flowerName}替你守住一点勇气。", 80),
                GeneratedAtUtcTicks = _clock?.UtcNow.Ticks ?? DateTime.UtcNow.Ticks
            };
        }

        private static string TrimSummary(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Math.Max(1, maxLength - 1)) + "…";
        }

        private static string ClusterKey(string emotionType, string owner) => $"{emotionType}|{owner}";
        private static string PlacementInventoryKey(string emotionType, string owner) => $"{emotionType}|{owner}";

        // ── Save container ─────────────────────────────────────

        [Serializable]
        private sealed class EmotionGardenSaveData
        {
            public int Version;
            public string LastSubmitDateIso = string.Empty;
            public List<EmotionFlowerData> Flowers = new();
            public List<ClusterProgress> Clusters = new();
            public List<PlacementFlowerInventory> PlacementInventories = new();
            public List<PlacedEmotionFlower> PlacedFlowers = new();
            public List<EmotionDailySummaryData> DailySummaries = new();
        }
    }
}
