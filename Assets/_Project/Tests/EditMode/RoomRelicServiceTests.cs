#nullable enable
using System;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using GeminiLab.Modules.RoomRelic;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class RoomRelicServiceTests
    {
        private FakeGameClock _clock = null!;
        private PetRoster _roster = null!;
        private PetSocialService _social = null!;
        private RoomRelicCatalogSO _catalog = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeGameClock();
            _clock.SetLocal(new DateTime(2026, 9, 3, 12, 0, 0));

            _roster = new PetRoster();
            _roster.Register(PetId.Angel, new PetRuntimeData { Energy = 80f, Mood = 60f });
            _roster.Register(PetId.Devil, new PetRuntimeData { Energy = 80f, Mood = 60f });
            _social = new PetSocialService(_roster, utcNow: () => _clock.UtcNow);

            _catalog = ScriptableObject.CreateInstance<RoomRelicCatalogSO>();
            _catalog.notes = new[]
            {
                new RoomNoteData { id = "note_demon_01", senderCharacter = "Demon", receiverCharacter = "Angel", content = "d1" },
                new RoomNoteData { id = "note_angel_01", senderCharacter = "Angel", receiverCharacter = "Demon", content = "a1" }
            };
            _catalog.relics = new[]
            {
                new RoomRelicData { id = "relic_demon_01", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "r1" },
                new RoomRelicData { id = "relic_angel_01", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "r2" }
            };
            _catalog.gifts = new[]
            {
                new RoomGiftData { id = "gift_demon_01", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "g1", displaySlotId = "desk" },
                new RoomGiftData { id = "gift_demon_02", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "g2", displaySlotId = "shelf" },
                new RoomGiftData { id = "gift_angel_01", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "g3", displaySlotId = "desk" }
            };
        }

        [Test]
        public void RoomEntry_IsIdempotentForSameDay()
        {
            _social.ApplySpecialEventFriendship(15f); // 30 -> 45
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());

            service.ProcessRoomEntry(RoomId.AngelRoom);
            RoomRelicData? first = service.GetCurrentRelic(RoomId.AngelRoom);

            service.ProcessRoomEntry(RoomId.AngelRoom);
            RoomRelicData? second = service.GetCurrentRelic(RoomId.AngelRoom);

            Assert.AreEqual(first?.id, second?.id);
        }

        [Test]
        public void Below45_DoesNotSpawnRelic()
        {
            _social.ApplySpecialEventFriendship(14f); // 30 -> 44
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());

            service.ProcessRoomEntry(RoomId.AngelRoom);

            Assert.IsNull(service.GetCurrentRelic(RoomId.AngelRoom));
        }

        [Test]
        public void Reaching45_ForcesFirstRelic()
        {
            _social.ApplySpecialEventFriendship(15f); // 30 -> 45
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());

            service.ProcessRoomEntry(RoomId.AngelRoom);

            Assert.IsNotNull(service.GetCurrentRelic(RoomId.AngelRoom));
        }

        [Test]
        public void Reaching80_ForcesFirstGift()
        {
            _social.ApplySpecialEventFriendship(50f); // 30 -> 80
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());

            service.ProcessRoomEntry(RoomId.AngelRoom);

            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void GiftPool_IsExhaustedWithoutDuplicates()
        {
            _social.ApplySpecialEventFriendship(50f); // 30 -> 80
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());

            service.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);

            _clock.Advance(TimeSpan.FromDays(1));
            service.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.AreEqual(2, service.GetPlacedGifts(RoomId.AngelRoom).Count);

            _clock.Advance(TimeSpan.FromDays(1));
            service.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.AreEqual(2, service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void Persistence_RoundTrip_PreservesGifts()
        {
            _social.ApplySpecialEventFriendship(50f); // 30 -> 80
            var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            string json = service.CaptureJson();

            var restored = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            Assert.IsTrue(restored.RestoreJson(json));
            Assert.AreEqual(1, restored.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void FirstGift_ReloadSameDay_DoesNotRollAgain()
        {
            _social.ApplySpecialEventFriendship(50f);
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            using var restored = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            Assert.IsTrue(restored.RestoreJson(service.CaptureJson()));
            restored.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.AreEqual(1, restored.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void ConsumedFirstRelic_ReloadSameDay_StaysConsumed()
        {
            _social.ApplySpecialEventFriendship(15f);
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            service.ConsumeCurrentItem(RoomId.AngelRoom, RoomRelicKind.TemporaryRelic);
            service.ConsumeCurrentItem(RoomId.AngelRoom, RoomRelicKind.Note);
            using var restored = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            Assert.IsTrue(restored.RestoreJson(service.CaptureJson()));
            restored.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.IsNull(restored.GetCurrentRelic(RoomId.AngelRoom));
            Assert.IsNull(restored.GetCurrentNote(RoomId.AngelRoom));
            _clock.Advance(TimeSpan.FromDays(1));
            restored.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.IsNotNull(restored.GetCurrentRelic(RoomId.AngelRoom));
            Assert.IsNotNull(restored.GetCurrentNote(RoomId.AngelRoom));
        }

        [Test]
        public void OldSave_FirstGiftDateMissing_DoesNotGrantExtraGift()
        {
            _social.ApplySpecialEventFriendship(50f);
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            string oldJson = "{\"version\":1,\"angelRoom\":{\"lastNoteRollDateIso\":\"2026-09-03\",\"giftUnlocked\":true},\"devilRoom\":{},\"obtainedGiftIds\":[\"gift_demon_01\"]}";
            Assert.IsTrue(service.RestoreJson(oldJson));
            service.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void ResumeAfterLeavingScene_RestoresThresholdEventsWithoutDuplicates()
        {
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.SetCurrentRoom(RoomId.AngelRoom);
            service.Dispose();
            service.Resume();
            service.Resume();
            service.ProcessRoomEntry(RoomId.AngelRoom);
            int giftEvents = 0;
            service.GiftObtained += _ => giftEvents++;
            _social.ApplySpecialEventFriendship(50f);
            Assert.AreEqual(1, giftEvents);
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);
            Assert.IsNull(service.GetCurrentRelic(RoomId.AngelRoom));
        }

        [Test]
        public void BothOccupiedRooms_UnlockIndependently()
        {
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            service.ProcessRoomEntry(RoomId.DevilRoom);
            _social.ApplySpecialEventFriendship(15f);
            Assert.IsNotNull(service.GetCurrentRelic(RoomId.AngelRoom));
            Assert.IsNotNull(service.GetCurrentRelic(RoomId.DevilRoom));
            service.ClearCurrentRoom(RoomId.DevilRoom);
            _social.ApplySpecialEventFriendship(35f);
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);
            Assert.AreEqual(0, service.GetPlacedGifts(RoomId.DevilRoom).Count);
            service.ProcessRoomEntry(RoomId.DevilRoom);
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.DevilRoom).Count);
        }

        [Test]
        public void Restore_NotifiesBothViewsWithoutNewGiftPopup()
        {
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            int stateEvents = 0;
            int giftEvents = 0;
            service.StateChanged += _ => stateEvents++;
            service.GiftObtained += _ => giftEvents++;
            Assert.IsTrue(service.RestoreJson(service.CaptureJson()));
            Assert.AreEqual(2, stateEvents);
            Assert.AreEqual(0, giftEvents);
        }

        [TestCase("{}")]
        [TestCase("{\"version\":99,\"angelRoom\":{},\"devilRoom\":{}}")]
        [TestCase("not-json")]
        public void InvalidSave_DoesNotOverwriteProgress(string json)
        {
            _social.ApplySpecialEventFriendship(50f);
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.IsFalse(service.RestoreJson(json));
            Assert.AreEqual(1, service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        [Test]
        public void FailedDailyChance_ClearsOldTemporaryItemsButKeepsGifts()
        {
            _social.ApplySpecialEventFriendship(50f);
            using var service = new RoomRelicService(_clock, _social, _catalog, new AlwaysSuccessRandom());
            service.ProcessRoomEntry(RoomId.AngelRoom);
            using var restored = new RoomRelicService(_clock, _social, _catalog, new NeverSuccessRandom());
            restored.RestoreJson(service.CaptureJson());
            _clock.Advance(TimeSpan.FromDays(1));
            restored.ProcessRoomEntry(RoomId.AngelRoom);
            Assert.IsNull(restored.GetCurrentNote(RoomId.AngelRoom));
            Assert.AreEqual(1, restored.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        private sealed class NeverSuccessRandom : System.Random
        {
            public override double NextDouble() => 0.999;
        }

        private sealed class AlwaysSuccessRandom : System.Random
        {
            public override double NextDouble() => 0.0;
        }
    }
}
