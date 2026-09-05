#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    public enum WorldMapWishState
    {
        Active = 0,
        Fulfilled = 1,
        Archived = 2
    }

    [Serializable]
    public sealed class WorldMapWishRecord
    {
        public string Id = string.Empty;
        public string Content = string.Empty;
        public string CreatedAtIso = string.Empty;
        public string FulfilledAtIso = string.Empty;
        public WorldMapWishState State = WorldMapWishState.Active;
        public int SlotIndex = -1;

        public DateTime CreatedAtUtc
        {
            get
            {
                return DateTime.TryParse(
                    CreatedAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsed)
                    ? parsed.ToUniversalTime()
                    : DateTime.MinValue;
            }
        }
    }

    [Serializable]
    internal sealed class WorldMapWishSaveData
    {
        public List<WorldMapWishRecord> Records = new();
    }

    /// <summary>
    /// WorldMap 许愿记录与持久化服务。只保存数据，不持有 UI 或场景引用。
    /// </summary>
    public sealed class WorldMapWishService
    {
        public const int SlotCount = 12;
        private const string PlayerPrefsKey = "geminilab.worldmap.wishes.v1";

        private readonly List<WorldMapWishRecord> _records = new();
        private readonly System.Random _random = new();

        public IReadOnlyList<WorldMapWishRecord> Records => _records;

        public WorldMapWishService()
        {
            Load();
        }

        public WorldMapWishRecord? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < _records.Count; i++)
            {
                if (string.Equals(_records[i].Id, id, StringComparison.Ordinal))
                {
                    return _records[i];
                }
            }

            return null;
        }

        public WorldMapWishRecord? GetVisibleAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return null;
            for (int i = 0; i < _records.Count; i++)
            {
                WorldMapWishRecord record = _records[i];
                if (record.SlotIndex == slotIndex && record.State != WorldMapWishState.Archived)
                {
                    return record;
                }
            }

            return null;
        }

        public WorldMapWishRecord? CreateWish(string content)
        {
            string normalized = (content ?? string.Empty).Trim();
            if (normalized.Length == 0) return null;

            WorldMapWishRecord? oldestActive = FindOldestVisible();
            if (oldestActive != null && CountVisible() >= SlotCount)
            {
                oldestActive.State = WorldMapWishState.Archived;
                oldestActive.SlotIndex = -1;
            }

            int slot = FindFreeSlot();
            if (slot < 0)
            {
                // Defensive fallback: the oldest visible item is always replaceable.
                oldestActive ??= FindOldestVisible();
                if (oldestActive == null) return null;
                oldestActive.State = WorldMapWishState.Archived;
                oldestActive.SlotIndex = -1;
                slot = FindFreeSlot();
            }

            DateTime now = DateTime.UtcNow;
            var record = new WorldMapWishRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Content = normalized,
                CreatedAtIso = now.ToString("O", CultureInfo.InvariantCulture),
                FulfilledAtIso = string.Empty,
                State = WorldMapWishState.Active,
                SlotIndex = slot
            };
            _records.Add(record);
            Save();
            return record;
        }

        public bool Fulfill(string id)
        {
            WorldMapWishRecord? record = GetById(id);
            if (record == null || record.State != WorldMapWishState.Active) return false;

            record.State = WorldMapWishState.Fulfilled;
            record.FulfilledAtIso = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            Save();
            return true;
        }

        public bool Archive(string id)
        {
            WorldMapWishRecord? record = GetById(id);
            if (record == null || record.State == WorldMapWishState.Archived) return false;

            record.State = WorldMapWishState.Archived;
            record.SlotIndex = -1;
            Save();
            return true;
        }

        private int CountVisible()
        {
            int count = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].State != WorldMapWishState.Archived &&
                    _records[i].SlotIndex >= 0 && _records[i].SlotIndex < SlotCount)
                {
                    count++;
                }
            }

            return count;
        }

        private int FindFreeSlot()
        {
            var occupied = new bool[SlotCount];
            for (int i = 0; i < _records.Count; i++)
            {
                int slot = _records[i].SlotIndex;
                if (_records[i].State != WorldMapWishState.Archived && slot >= 0 && slot < SlotCount)
                {
                    occupied[slot] = true;
                }
            }

            var free = new List<int>();
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (!occupied[slot]) free.Add(slot);
            }

            return free.Count == 0 ? -1 : free[_random.Next(free.Count)];
        }

        private WorldMapWishRecord? FindOldestVisible()
        {
            WorldMapWishRecord? oldest = null;
            for (int i = 0; i < _records.Count; i++)
            {
                WorldMapWishRecord current = _records[i];
                if (current.State == WorldMapWishState.Archived) continue;
                if (oldest == null || current.CreatedAtUtc < oldest.CreatedAtUtc) oldest = current;
            }

            return oldest;
        }

        private void Load()
        {
            _records.Clear();
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                WorldMapWishSaveData? data = JsonUtility.FromJson<WorldMapWishSaveData>(json);
                if (data?.Records == null) return;
                for (int i = 0; i < data.Records.Count; i++)
                {
                    WorldMapWishRecord? record = data.Records[i];
                    if (record == null || string.IsNullOrWhiteSpace(record.Id) || string.IsNullOrWhiteSpace(record.Content))
                    {
                        continue;
                    }

                    if (record.State == WorldMapWishState.Archived) record.SlotIndex = -1;
                    _records.Add(record);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[WorldMapWishService] 读取愿望存档失败，将使用空记录：{exception.Message}");
            }
        }

        private void Save()
        {
            var data = new WorldMapWishSaveData { Records = _records };
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
