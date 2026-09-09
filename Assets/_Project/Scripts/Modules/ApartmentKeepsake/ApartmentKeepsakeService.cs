#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.ApartmentKeepsake
{
    /// <summary>
    /// 室内纸条、临时遗留物与永久赠礼的纯业务实现。
    /// 每日规则只读取 <see cref="IGameClock"/>，不持有任何 Scene 或 UI 对象。
    /// </summary>
    public sealed class ApartmentKeepsakeService : IApartmentKeepsakeService, IPersistentService
    {
        public const string PersistenceKey = "apartment_keepsake";
        public const float MementoRelationThreshold = 45f;
        public const float GiftRelationThreshold = 80f;
        public const double NoteSpawnProbability = 0.50d;
        public const double MementoSpawnProbability = 0.50d;
        public const double GiftSpawnProbability = 0.15d;

        private const int SaveVersion = 1;
        private const int NoteSpawnPointCount = 4;

        private static readonly string[] s_angelToDevilNotes =
        {
            "窗边给你留了位置。别误会，只是那里今天很暖。",
            "记得休息。逞强并不会让今天变得更长。",
            "我把你总找不到的东西放回原位了。",
            "晚一点也没关系，我会替你留一盏灯。"
        };

        private static readonly string[] s_devilToAngelNotes =
        {
            "桌上的点心不是特意留给你的，只是刚好多了一份。",
            "别又忙到忘记时间。今天必须准点回来。",
            "你喜欢的那首歌我只听了一遍，真的。",
            "窗户我关好了。省得你回来又说风太大。"
        };

        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;
        private readonly IApartmentKeepsakeRandom _random;
        private readonly List<ApartmentGiftRecord> _ownedGifts = new();

        private ApartmentNoteState _currentNote;
        private ApartmentMementoState _currentMemento;
        private string _lastIndoorRollDateIso = string.Empty;
        private string _lastMementoRollDateIso = string.Empty;
        private string _lastGiftRollDateIso = string.Empty;
        private bool _mementoFirstGuaranteeGranted;
        private bool _angelGiftGuaranteeGranted;
        private bool _devilGiftGuaranteeGranted;

        public ApartmentKeepsakeService(
            IGameClock clock,
            EventBus? eventBus = null,
            IApartmentKeepsakeRandom? random = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            _random = random ?? new SystemApartmentKeepsakeRandom();
            _currentNote = EmptyNote();
            _currentMemento = EmptyMemento();
        }

        public string Key => PersistenceKey;
        public ApartmentNoteState CurrentNote => _currentNote;
        public ApartmentMementoState CurrentMemento => _currentMemento;
        public IReadOnlyList<ApartmentGiftRecord> OwnedGifts => _ownedGifts;
        public string LastIndoorRollDateIso => _lastIndoorRollDateIso;

        public bool ProcessFirstIndoorEntry(float angelRelation, float devilRelation)
        {
            string today = _clock.TodayIso;
            if (string.Equals(_lastIndoorRollDateIso, today, StringComparison.Ordinal))
            {
                return false;
            }

            _lastIndoorRollDateIso = today;
            RefreshNote();
            RefreshMemento(angelRelation, devilRelation, today);
            RefreshGifts(angelRelation, devilRelation, today);
            PublishChanged(requestAutosave: true);
            return true;
        }

        public bool ProcessRelationThreshold(ApartmentKeepsakeOwner owner, float previousRelation, float currentRelation)
        {
            if (!IsKnownOwner(owner) || float.IsNaN(previousRelation) || float.IsNaN(currentRelation))
            {
                return false;
            }

            bool changed = false;
            string today = _clock.TodayIso;

            bool crossedGiftThreshold = previousRelation < GiftRelationThreshold &&
                                        currentRelation >= GiftRelationThreshold;
            if (crossedGiftThreshold)
            {
                if (_currentMemento.IsPresent && _currentMemento.Owner == owner)
                {
                    _currentMemento = EmptyMemento();
                    changed = true;
                }

                if (!IsGiftGuaranteeGranted(owner))
                {
                    SetGiftGuaranteeGranted(owner);
                    GrantRandomGift(owner, today);
                    _lastGiftRollDateIso = today;
                    changed = true;
                }
            }
            else
            {
                bool crossedMementoThreshold = previousRelation < MementoRelationThreshold &&
                                                currentRelation >= MementoRelationThreshold &&
                                                currentRelation < GiftRelationThreshold;
                if (crossedMementoThreshold && !_mementoFirstGuaranteeGranted)
                {
                    _mementoFirstGuaranteeGranted = true;
                    _lastMementoRollDateIso = today;
                    _currentMemento = CreateRandomMemento(owner);
                    changed = true;
                }
            }

            if (changed)
            {
                PublishChanged(requestAutosave: true);
            }

            return changed;
        }

        public bool IsGiftOwned(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            for (int i = 0; i < _ownedGifts.Count; i++)
            {
                if (string.Equals(_ownedGifts[i].ItemId, itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload
            {
                Version = SaveVersion,
                CurrentNote = _currentNote,
                CurrentMemento = _currentMemento,
                OwnedGifts = _ownedGifts.ToArray(),
                LastIndoorRollDateIso = _lastIndoorRollDateIso,
                LastMementoRollDateIso = _lastMementoRollDateIso,
                LastGiftRollDateIso = _lastGiftRollDateIso,
                MementoFirstGuaranteeGranted = _mementoFirstGuaranteeGranted,
                AngelGiftGuaranteeGranted = _angelGiftGuaranteeGranted,
                DevilGiftGuaranteeGranted = _devilGiftGuaranteeGranted
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
                !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                SavePayload? payload = JsonUtility.FromJson<SavePayload>(trimmed);
                if (payload is null)
                {
                    return false;
                }

                ApartmentNoteState restoredNote = NormalizeNote(payload.CurrentNote);
                ApartmentMementoState restoredMemento = NormalizeMemento(payload.CurrentMemento);
                var restoredGifts = NormalizeGifts(payload.OwnedGifts);

                _currentNote = restoredNote;
                _currentMemento = restoredMemento;
                _ownedGifts.Clear();
                _ownedGifts.AddRange(restoredGifts);
                _lastIndoorRollDateIso = payload.LastIndoorRollDateIso ?? string.Empty;
                _lastMementoRollDateIso = payload.LastMementoRollDateIso ?? string.Empty;
                _lastGiftRollDateIso = payload.LastGiftRollDateIso ?? string.Empty;
                _mementoFirstGuaranteeGranted = payload.MementoFirstGuaranteeGranted || restoredMemento.IsPresent;
            _angelGiftGuaranteeGranted = payload.AngelGiftGuaranteeGranted || HasGiftOwnedBy(ApartmentKeepsakeOwner.Angel);
            _devilGiftGuaranteeGranted = payload.DevilGiftGuaranteeGranted || HasGiftOwnedBy(ApartmentKeepsakeOwner.Devil);

                PublishChanged(requestAutosave: false);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ApartmentKeepsakeService] 存档恢复失败：{exception.Message}");
                return false;
            }
        }

        private void RefreshNote()
        {
            if (_random.NextUnit() >= NoteSpawnProbability)
            {
                _currentNote = EmptyNote();
                return;
            }

            bool angelSends = NextIndex(2) == 0;
            ApartmentKeepsakeOwner sender = angelSends ? ApartmentKeepsakeOwner.Angel : ApartmentKeepsakeOwner.Devil;
            ApartmentKeepsakeOwner recipient = angelSends ? ApartmentKeepsakeOwner.Devil : ApartmentKeepsakeOwner.Angel;
            string[] pool = angelSends ? s_angelToDevilNotes : s_devilToAngelNotes;
            int contentIndex = NextIndex(pool.Length);
            _currentNote = new ApartmentNoteState
            {
                IsPresent = true,
                NoteId = $"note.{sender.ToString().ToLowerInvariant()}_to_{recipient.ToString().ToLowerInvariant()}.{contentIndex}",
                Sender = sender,
                Recipient = recipient,
                Content = pool[contentIndex],
                SpawnIndex = NextIndex(NoteSpawnPointCount)
            };
        }

        private void RefreshMemento(float angelRelation, float devilRelation, string today)
        {
            bool angelEligible = IsMementoRelation(angelRelation);
            bool devilEligible = IsMementoRelation(devilRelation);
            if (!angelEligible && !devilEligible)
            {
                _currentMemento = EmptyMemento();
                _lastMementoRollDateIso = today;
                return;
            }

            if (!_mementoFirstGuaranteeGranted)
            {
            ApartmentKeepsakeOwner guaranteedOwner = ChooseOwner(angelEligible, devilEligible);
                _currentMemento = CreateRandomMemento(guaranteedOwner);
                _mementoFirstGuaranteeGranted = true;
                _lastMementoRollDateIso = today;
                return;
            }

            if (string.Equals(_lastMementoRollDateIso, today, StringComparison.Ordinal))
            {
                return;
            }

            _lastMementoRollDateIso = today;
            if (_random.NextUnit() >= MementoSpawnProbability)
            {
                _currentMemento = EmptyMemento();
                return;
            }

            _currentMemento = CreateRandomMemento(ChooseOwner(angelEligible, devilEligible));
        }

        private void RefreshGifts(float angelRelation, float devilRelation, string today)
        {
            bool angelEligible = angelRelation >= GiftRelationThreshold;
            bool devilEligible = devilRelation >= GiftRelationThreshold;
            if (!angelEligible && !devilEligible)
            {
                return;
            }

            if (string.Equals(_lastGiftRollDateIso, today, StringComparison.Ordinal))
            {
                return;
            }

            bool grantedGuarantee = false;
            if (angelEligible && !_angelGiftGuaranteeGranted)
            {
                _angelGiftGuaranteeGranted = true;
                GrantRandomGift(ApartmentKeepsakeOwner.Angel, today);
                grantedGuarantee = true;
            }

            if (devilEligible && !_devilGiftGuaranteeGranted)
            {
                _devilGiftGuaranteeGranted = true;
                GrantRandomGift(ApartmentKeepsakeOwner.Devil, today);
                grantedGuarantee = true;
            }

            _lastGiftRollDateIso = today;
            if (grantedGuarantee || _random.NextUnit() >= GiftSpawnProbability)
            {
                return;
            }

            var candidates = new List<ApartmentKeepsakeItemDefinition>();
            if (angelEligible)
            {
                AddUnownedGiftCandidates(ApartmentKeepsakeOwner.Angel, candidates);
            }

            if (devilEligible)
            {
                AddUnownedGiftCandidates(ApartmentKeepsakeOwner.Devil, candidates);
            }

            if (candidates.Count > 0)
            {
                AddGift(candidates[NextIndex(candidates.Count)], today);
            }
        }

        private ApartmentMementoState CreateRandomMemento(ApartmentKeepsakeOwner owner)
        {
            IReadOnlyList<ApartmentKeepsakeItemDefinition> pool =
                ApartmentKeepsakeCatalog.GetPool(owner, ApartmentKeepsakeItemKind.Memento);
            if (pool.Count == 0)
            {
                return EmptyMemento();
            }

            ApartmentKeepsakeItemDefinition item = pool[NextIndex(pool.Count)];
            return new ApartmentMementoState
            {
                IsPresent = true,
                ItemId = item.Id,
                Owner = owner
            };
        }

        private bool GrantRandomGift(ApartmentKeepsakeOwner owner, string today)
        {
            var candidates = new List<ApartmentKeepsakeItemDefinition>();
            AddUnownedGiftCandidates(owner, candidates);
            if (candidates.Count == 0)
            {
                return false;
            }

            AddGift(candidates[NextIndex(candidates.Count)], today);
            return true;
        }

        private void AddUnownedGiftCandidates(
            ApartmentKeepsakeOwner owner,
            List<ApartmentKeepsakeItemDefinition> candidates)
        {
            IReadOnlyList<ApartmentKeepsakeItemDefinition> pool =
                ApartmentKeepsakeCatalog.GetPool(owner, ApartmentKeepsakeItemKind.Gift);
            for (int i = 0; i < pool.Count; i++)
            {
                if (!IsGiftOwned(pool[i].Id))
                {
                    candidates.Add(pool[i]);
                }
            }
        }

        private void AddGift(ApartmentKeepsakeItemDefinition definition, string today)
        {
            if (IsGiftOwned(definition.Id))
            {
                return;
            }

            _ownedGifts.Add(new ApartmentGiftRecord
            {
                ItemId = definition.Id,
                Owner = definition.Owner,
                AcquiredDateIso = today
            });
        }

        private bool HasGiftOwnedBy(ApartmentKeepsakeOwner owner)
        {
            for (int i = 0; i < _ownedGifts.Count; i++)
            {
                if (_ownedGifts[i].Owner == owner)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsGiftGuaranteeGranted(ApartmentKeepsakeOwner owner)
        {
            return owner == ApartmentKeepsakeOwner.Angel
                ? _angelGiftGuaranteeGranted
                : _devilGiftGuaranteeGranted;
        }

        private void SetGiftGuaranteeGranted(ApartmentKeepsakeOwner owner)
        {
            if (owner == ApartmentKeepsakeOwner.Angel)
            {
                _angelGiftGuaranteeGranted = true;
            }
            else
            {
                _devilGiftGuaranteeGranted = true;
            }
        }

        private ApartmentKeepsakeOwner ChooseOwner(bool angelEligible, bool devilEligible)
        {
            if (angelEligible && devilEligible)
            {
                return NextIndex(2) == 0 ? ApartmentKeepsakeOwner.Angel : ApartmentKeepsakeOwner.Devil;
            }

            return devilEligible ? ApartmentKeepsakeOwner.Devil : ApartmentKeepsakeOwner.Angel;
        }

        private int NextIndex(int exclusiveMax)
        {
            if (exclusiveMax <= 1)
            {
                return 0;
            }

            int value = _random.NextIndex(exclusiveMax);
            int normalized = value % exclusiveMax;
            return normalized < 0 ? normalized + exclusiveMax : normalized;
        }

        private void PublishChanged(bool requestAutosave)
        {
            _eventBus?.Publish(new ApartmentKeepsakeStateChangedEvent(requestAutosave));
        }

        private static bool IsKnownOwner(ApartmentKeepsakeOwner owner)
        {
            return owner == ApartmentKeepsakeOwner.Angel || owner == ApartmentKeepsakeOwner.Devil;
        }

        private static bool IsMementoRelation(float relation)
        {
            return relation >= MementoRelationThreshold && relation < GiftRelationThreshold;
        }

        private static ApartmentNoteState EmptyNote()
        {
            return new ApartmentNoteState
            {
                IsPresent = false,
                NoteId = string.Empty,
                Sender = ApartmentKeepsakeOwner.Angel,
                Recipient = ApartmentKeepsakeOwner.Devil,
                Content = string.Empty,
                SpawnIndex = 0
            };
        }

        private static ApartmentMementoState EmptyMemento()
        {
            return new ApartmentMementoState
            {
                IsPresent = false,
                ItemId = string.Empty,
                Owner = ApartmentKeepsakeOwner.Angel
            };
        }

        private static ApartmentNoteState NormalizeNote(ApartmentNoteState note)
        {
            if (!note.IsPresent ||
                string.IsNullOrWhiteSpace(note.NoteId) ||
                string.IsNullOrWhiteSpace(note.Content) ||
                !IsKnownOwner(note.Sender) ||
                !IsKnownOwner(note.Recipient) ||
                note.Sender == note.Recipient)
            {
                return EmptyNote();
            }

            note.NoteId = note.NoteId.Trim();
            note.Content = note.Content.Trim();
            int spawnIndex = note.SpawnIndex % NoteSpawnPointCount;
            note.SpawnIndex = spawnIndex < 0 ? spawnIndex + NoteSpawnPointCount : spawnIndex;
            return note;
        }

        private static ApartmentMementoState NormalizeMemento(ApartmentMementoState memento)
        {
            if (!memento.IsPresent ||
                string.IsNullOrWhiteSpace(memento.ItemId) ||
                !ApartmentKeepsakeCatalog.TryGet(memento.ItemId.Trim(), out ApartmentKeepsakeItemDefinition item) ||
                item.Kind != ApartmentKeepsakeItemKind.Memento ||
                item.Owner != memento.Owner)
            {
                return EmptyMemento();
            }

            memento.ItemId = item.Id;
            return memento;
        }

        private static List<ApartmentGiftRecord> NormalizeGifts(ApartmentGiftRecord[]? records)
        {
            var result = new List<ApartmentGiftRecord>();
            if (records is null)
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < records.Length; i++)
            {
                ApartmentGiftRecord record = records[i];
                string itemId = record.ItemId?.Trim() ?? string.Empty;
                if (!ApartmentKeepsakeCatalog.TryGet(itemId, out ApartmentKeepsakeItemDefinition item) ||
                    item.Kind != ApartmentKeepsakeItemKind.Gift ||
                    item.Owner != record.Owner ||
                    !seen.Add(item.Id))
                {
                    continue;
                }

                record.ItemId = item.Id;
                record.AcquiredDateIso ??= string.Empty;
                result.Add(record);
            }

            return result;
        }

        [Serializable]
        private sealed class SavePayload
        {
            public int Version;
            public ApartmentNoteState CurrentNote;
            public ApartmentMementoState CurrentMemento;
            public ApartmentGiftRecord[]? OwnedGifts;
            public string? LastIndoorRollDateIso;
            public string? LastMementoRollDateIso;
            public string? LastGiftRollDateIso;
            public bool MementoFirstGuaranteeGranted;
            public bool AngelGiftGuaranteeGranted;
            public bool DevilGiftGuaranteeGranted;
        }

        private sealed class SystemApartmentKeepsakeRandom : IApartmentKeepsakeRandom
        {
            private readonly System.Random _random = new();

            public double NextUnit() => _random.NextDouble();

            public int NextIndex(int exclusiveMax)
            {
                return exclusiveMax <= 1 ? 0 : _random.Next(exclusiveMax);
            }
        }
    }
}
