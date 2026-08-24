#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Modules.Apple;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GeminiLab.Modules.Collection
{
    public sealed class GachaService : IGachaService, IPersistentService
    {
        public const int SingleCost = 20;
        public const int MultiCost = SingleCost * MultiCount;
        private const int MultiCount = 5;
        private const int DuplicateRefund = 30;

        public static readonly string[] AllCollectibleIds =
        {
            "acrylic_sign", "photo", "polaroid", "postcard", "sticker",
            "angel_badge", "evil_badge"
        };

        public static readonly Dictionary<string, string> CollectibleTags = new()
        {
            { "acrylic_sign", "partner_tag" },
            { "photo", "partner_tag" },
            { "polaroid", "partner_tag" },
            { "postcard", "partner_tag" },
            { "sticker", "partner_tag" },
            { "angel_badge", "angel_tag" },
            { "evil_badge", "devil_tag" }
        };

        public static readonly Dictionary<string, string> CollectibleNames = new()
        {
            { "acrylic_sign", "Acrylic sign" },
            { "photo", "photo" },
            { "polaroid", "Polaroid" },
            { "postcard", "postcard" },
            { "sticker", "sticker" },
            { "angel_badge", "angel_badge" },
            { "evil_badge", "evil_badge" }
        };

        private readonly IAppleService _apple;
        private readonly ICoinService _coin;
        private readonly ICollectionService _collection;
        private readonly EventBus? _eventBus;
        private readonly HashSet<string> _unlocked = new();

        public GachaService(IAppleService apple, ICoinService coin, ICollectionService collection, EventBus? eventBus)
        {
            _apple = apple ?? throw new ArgumentNullException(nameof(apple));
            _coin = coin ?? throw new ArgumentNullException(nameof(coin));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _eventBus = eventBus;
        }

        public string Key => "gacha";

        public IReadOnlyList<string> UnlockedIds => _unlocked.ToList();

        public bool IsUnlocked(string collectibleId) => _unlocked.Contains(collectibleId);

        public int CurrentCost(int count) => count switch { 1 => SingleCost, 5 => MultiCost, _ => Mathf.Max(0, count) * SingleCost };

        public bool CanPull(int count)
        {
            int cost = CurrentCost(count);
            return _apple.Balance >= cost;
        }

        public GachaResult PullSingle() => Pull(1);

        public GachaResult PullMulti(int count) => Pull(count <= 0 ? MultiCount : count);

        private GachaResult Pull(int count)
        {
            int cost = CurrentCost(count);
            if (!_apple.TrySpend(cost))
            {
                return new GachaResult(Array.Empty<GachaItem>(), 0);
            }

            var items = new GachaItem[count];
            int refund = 0;

            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, AllCollectibleIds.Length);
                string id = AllCollectibleIds[idx];
                bool isNew = _unlocked.Add(id);

                if (isNew)
                {
                    var entry = CreateCollectionEntry(id);
                    _collection.Add(entry);
                }
                else
                {
                    refund += DuplicateRefund;
                }

                items[i] = new GachaItem(id, isNew);
            }

            if (refund > 0)
            {
                _coin.Add(refund);
            }

            var result = new GachaResult(items, refund);
            _eventBus?.Publish(new GachaPullEvent(result));
            return result;
        }

        private CollectionEntry CreateCollectionEntry(string id)
        {
            CollectibleTags.TryGetValue(id, out string? tag);
            CollectibleNames.TryGetValue(id, out string? name);
            return new CollectionEntry
            {
                Id = $"gacha_{id}",
                Category = CollectionCategory.GachaCollectible,
                Title = name ?? id,
                Description = tag ?? string.Empty,
                AcquiredDateIso = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                IconKey = $"gacha_{id}"
            };
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct SavePayload
        {
            public int version;
            public string[] unlockedIds;
        }

        public string CaptureJson()
        {
            var ids = new string[_unlocked.Count];
            _unlocked.CopyTo(ids);
            return JsonUtility.ToJson(new SavePayload { version = 1, unlockedIds = ids });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _unlocked.Clear();
                if (payload.unlockedIds != null)
                {
                    foreach (var id in payload.unlockedIds)
                    {
                        if (!string.IsNullOrEmpty(id)) _unlocked.Add(id);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
