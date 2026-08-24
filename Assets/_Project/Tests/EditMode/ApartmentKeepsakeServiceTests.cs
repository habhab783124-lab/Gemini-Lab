#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.ApartmentKeepsake;
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ApartmentKeepsakeServiceTests
    {
        private FakeGameClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeGameClock();
            _clock.SetLocal(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Local));
        }

        [Test]
        public void NoteProbability_UsesStrictFiftyPercentBoundaryAndRefreshesNextDay()
        {
            var random = new SequenceRandom();
            var service = new ApartmentKeepsakeService(_clock, random: random);

            random.Units.Enqueue(0.50d);
            Assert.IsTrue(service.ProcessFirstIndoorEntry(44f, 44f));
            Assert.IsFalse(service.CurrentNote.IsPresent);

            int unitCallsAfterFirstEntry = random.UnitCallCount;
            Assert.IsFalse(service.ProcessFirstIndoorEntry(44f, 44f), "同日重复进入不能重新抽取");
            Assert.AreEqual(unitCallsAfterFirstEntry, random.UnitCallCount);

            _clock.Advance(TimeSpan.FromDays(1));
            random.Units.Enqueue(0.499999d);
            random.Indexes.Enqueue(1); // Devil -> Angel
            random.Indexes.Enqueue(2); // 对应文本
            random.Indexes.Enqueue(3); // 随机位置
            Assert.IsTrue(service.ProcessFirstIndoorEntry(44f, 44f));
            Assert.IsTrue(service.CurrentNote.IsPresent);
            Assert.AreEqual(ApartmentKeepsakeOwner.Devil, service.CurrentNote.Sender);
            Assert.AreEqual(ApartmentKeepsakeOwner.Angel, service.CurrentNote.Recipient);
            Assert.AreEqual(3, service.CurrentNote.SpawnIndex);
            Assert.That(service.CurrentNote.Content, Is.Not.Empty);
        }

        [Test]
        public void Memento_UnlocksAtFortyFivePersistsThroughSeventyNineAndLeavesTierAtEighty()
        {
            var random = new SequenceRandom(indexes: new[] { 1, 0 });
            var service = new ApartmentKeepsakeService(_clock, random: random);

            Assert.IsFalse(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 43f, 44f));
            Assert.IsFalse(service.CurrentMemento.IsPresent);

            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 44f, 45f));
            Assert.IsTrue(service.CurrentMemento.IsPresent);
            Assert.AreEqual(ApartmentKeepsakeOwner.Angel, service.CurrentMemento.Owner);
            Assert.IsTrue(service.CurrentMemento.ItemId.StartsWith("memento.angel.", StringComparison.Ordinal));

            Assert.IsFalse(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 45f, 79f));
            Assert.IsTrue(service.CurrentMemento.IsPresent);

            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 79f, 80f));
            Assert.IsFalse(service.CurrentMemento.IsPresent);
            Assert.AreEqual(1, service.OwnedGifts.Count);
            Assert.AreEqual(ApartmentKeepsakeOwner.Angel, service.OwnedGifts[0].Owner);
        }

        [Test]
        public void MementoDailyRoll_UsesStrictFiftyPercentBoundary()
        {
            var random = new SequenceRandom(indexes: new[] { 0 });
            var service = new ApartmentKeepsakeService(_clock, random: random);
            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 44f, 45f));

            // 阈值保底当天不允许每日刷新立刻覆盖保底结果。
            random.Units.Enqueue(0.50d); // note
            Assert.IsTrue(service.ProcessFirstIndoorEntry(45f, 44f));
            Assert.IsTrue(service.CurrentMemento.IsPresent);

            _clock.Advance(TimeSpan.FromDays(1));
            random.Units.Enqueue(0.50d); // note
            random.Units.Enqueue(0.50d); // memento: 边界不出现
            Assert.IsTrue(service.ProcessFirstIndoorEntry(45f, 44f));
            Assert.IsFalse(service.CurrentMemento.IsPresent);

            _clock.Advance(TimeSpan.FromDays(1));
            random.Units.Enqueue(0.50d); // note
            random.Units.Enqueue(0.499999d); // memento: 出现
            random.Indexes.Enqueue(2);
            Assert.IsTrue(service.ProcessFirstIndoorEntry(45f, 44f));
            Assert.IsTrue(service.CurrentMemento.IsPresent);
        }

        [Test]
        public void Gift_FirstPerOwnerIsGuaranteedThenDailyRollUsesStrictFifteenPercentBoundary()
        {
            var random = new SequenceRandom(indexes: new[] { 0, 0, 0 });
            var service = new ApartmentKeepsakeService(_clock, random: random);

            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 79f, 80f));
            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Devil, 79f, 80f));
            Assert.AreEqual(2, service.OwnedGifts.Count, "每个角色达到 80 都应各有一次首次保底");

            random.Units.Enqueue(0.50d); // note；赠礼保底当天不再进行 15% 抽取
            Assert.IsTrue(service.ProcessFirstIndoorEntry(80f, 80f));
            Assert.AreEqual(2, service.OwnedGifts.Count);

            _clock.Advance(TimeSpan.FromDays(1));
            random.Units.Enqueue(0.50d); // note
            random.Units.Enqueue(0.15d); // gift: 边界不命中
            Assert.IsTrue(service.ProcessFirstIndoorEntry(80f, 80f));
            Assert.AreEqual(2, service.OwnedGifts.Count);

            _clock.Advance(TimeSpan.FromDays(1));
            random.Units.Enqueue(0.50d); // note
            random.Units.Enqueue(0.149999d); // gift: 命中
            random.Indexes.Enqueue(0);
            Assert.IsTrue(service.ProcessFirstIndoorEntry(80f, 80f));
            Assert.AreEqual(3, service.OwnedGifts.Count);
            Assert.AreEqual(3, UniqueGiftCount(service.OwnedGifts));
        }

        [Test]
        public void GiftPoolExhaustion_NeverDuplicatesPermanentGifts()
        {
            var random = new SequenceRandom(indexes: new[] { 0 });
            var service = new ApartmentKeepsakeService(_clock, random: random);
            Assert.IsTrue(service.ProcessRelationThreshold(ApartmentKeepsakeOwner.Angel, 79f, 80f));

            // Angel 池共三件；连续命中后，池耗尽仍保持三件且不重复。
            for (int day = 0; day < 3; day++)
            {
                _clock.Advance(TimeSpan.FromDays(1));
                random.Units.Enqueue(0.50d); // note
                random.Units.Enqueue(0d); // gift
                random.Indexes.Enqueue(0);
                Assert.IsTrue(service.ProcessFirstIndoorEntry(80f, 44f));
            }

            Assert.AreEqual(3, service.OwnedGifts.Count);
            Assert.AreEqual(3, UniqueGiftCount(service.OwnedGifts));
            foreach (ApartmentGiftRecord gift in service.OwnedGifts)
            {
                Assert.AreEqual(ApartmentKeepsakeOwner.Angel, gift.Owner);
                Assert.IsTrue(service.IsGiftOwned(gift.ItemId));
            }
        }

        [Test]
        public void SaveRestore_RoundTripsStatePublishesNonAutosaveEventAndRejectsBadJsonAtomically()
        {
            var sourceRandom = new SequenceRandom(
                units: new[] { 0d },
                indexes: new[] { 0, 1, 2, 1, 0 });
            var source = new ApartmentKeepsakeService(_clock, random: sourceRandom);
            Assert.IsTrue(source.ProcessFirstIndoorEntry(45f, 44f));
            Assert.IsTrue(source.ProcessRelationThreshold(ApartmentKeepsakeOwner.Devil, 79f, 80f));
            string saved = source.CaptureJson();

            var eventBus = new EventBus();
            ApartmentKeepsakeStateChangedEvent? restoredEvent = null;
            eventBus.Subscribe<ApartmentKeepsakeStateChangedEvent>(evt => restoredEvent = evt);
            var restored = new ApartmentKeepsakeService(_clock, eventBus, new SequenceRandom());

            Assert.IsTrue(restored.RestoreJson(saved));
            Assert.IsTrue(restoredEvent.HasValue);
            Assert.IsFalse(restoredEvent.Value.RequestAutosave, "读档刷新 UI 不能反向触发 autosave");
            Assert.AreEqual(source.LastIndoorRollDateIso, restored.LastIndoorRollDateIso);
            Assert.AreEqual(source.CurrentNote.NoteId, restored.CurrentNote.NoteId);
            Assert.AreEqual(source.CurrentMemento.ItemId, restored.CurrentMemento.ItemId);
            Assert.AreEqual(source.OwnedGifts.Count, restored.OwnedGifts.Count);

            string beforeBadRestore = restored.CaptureJson();
            Assert.IsFalse(restored.RestoreJson("{ definitely broken json"));
            Assert.AreEqual(beforeBadRestore, restored.CaptureJson(), "坏 JSON 不能破坏当前运行态");

            int giftCountBeforeSameDayEntry = restored.OwnedGifts.Count;
            Assert.IsFalse(restored.ProcessFirstIndoorEntry(45f, 80f), "恢复判定日期后，同日不能二次抽取");
            Assert.AreEqual(giftCountBeforeSameDayEntry, restored.OwnedGifts.Count);
        }

        [Test]
        public void PetRuntimeSave_VersionTwoPersistsRelationAndVersionOneKeepsCurrentDefault()
        {
            var roster = new PetRoster();
            var runtime = new PetRuntimeData
            {
                PetId = PetId.Angel,
                Mood = 61f,
                Energy = 72f,
                Satiety = 83f,
                Relation = 74f
            };
            roster.Register(PetId.Angel, runtime);
            var saveService = new PetRuntimeSaveService(roster);

            string versionTwoJson = saveService.CaptureJson();
            StringAssert.Contains("\"version\":2", versionTwoJson);
            runtime.Relation = 12f;
            Assert.IsTrue(saveService.RestoreJson(versionTwoJson));
            Assert.AreEqual(74f, runtime.Relation);

            runtime.Relation = 57f;
            const string versionOneJson =
                "{\"version\":1,\"entries\":[{\"petId\":0,\"mood\":40,\"energy\":50,\"satiety\":60}]}";
            Assert.IsTrue(saveService.RestoreJson(versionOneJson));
            Assert.AreEqual(57f, runtime.Relation, "v1 缺少 Relation 时必须保留当前默认/运行值");
            Assert.AreEqual(40f, runtime.Mood);
        }

        private static int UniqueGiftCount(IReadOnlyList<ApartmentGiftRecord> gifts)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < gifts.Count; i++)
            {
                ids.Add(gifts[i].ItemId);
            }

            return ids.Count;
        }

        private sealed class SequenceRandom : IApartmentKeepsakeRandom
        {
            public SequenceRandom(IEnumerable<double>? units = null, IEnumerable<int>? indexes = null)
            {
                Units = units is null ? new Queue<double>() : new Queue<double>(units);
                Indexes = indexes is null ? new Queue<int>() : new Queue<int>(indexes);
            }

            public Queue<double> Units { get; }
            public Queue<int> Indexes { get; }
            public int UnitCallCount { get; private set; }
            public int IndexCallCount { get; private set; }

            public double NextUnit()
            {
                UnitCallCount++;
                return Units.Count > 0 ? Units.Dequeue() : 1d;
            }

            public int NextIndex(int exclusiveMax)
            {
                IndexCallCount++;
                return Indexes.Count > 0 ? Indexes.Dequeue() : 0;
            }
        }
    }
}
