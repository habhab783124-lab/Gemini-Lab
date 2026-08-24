#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.EmotionGarden;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class EmotionGardenPlacementPersistenceTests
    {
        private GameObject _host = null!;
        private EmotionGardenService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("EmotionGardenPlacementPersistenceTests");
            _service = _host.AddComponent<EmotionGardenService>();
            _service.Initialize(new FixedClock(), new EventBus());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
        }

        [Test]
        public void CaptureAndRestore_RoundTripsPlacedFlowerWithoutAdditionalInventoryCost()
        {
            Restore(@"{""Version"":3,""LastSubmitDateIso"":""2026-08-11"",""Flowers"":[],""Clusters"":[],""PlacementInventories"":[{""EmotionType"":""喜悦"",""Owner"":""angel"",""SingleCount"":2,""ClusterCount"":0}]}");

            Assert.True(_service.TryPlaceFlower("喜悦", "angel", false, 4, 1.25f, -2.5f));
            Assert.AreEqual(1, _service.GetPlacementInventory("喜悦", "angel").SingleCount);

            string captured = ((IPersistentService)_service).CaptureJson();
            Restore(captured);

            PlacementFlowerInventory inventory = _service.GetPlacementInventory("喜悦", "angel");
            Assert.AreEqual(1, inventory.SingleCount);
            Assert.AreEqual(0, inventory.ClusterCount);
            Assert.AreEqual(1, _service.GetPlacedFlowers().Count);

            PlacedEmotionFlower placed = _service.GetPlacedFlowers()[0];
            Assert.AreEqual(4, placed.SlotIndex);
            Assert.AreEqual("喜悦", placed.EmotionType);
            Assert.AreEqual("angel", placed.Owner);
            Assert.False(placed.IsCluster);
            Assert.AreEqual(1.25f, placed.WorldX);
            Assert.AreEqual(-2.5f, placed.WorldY);
        }

        [Test]
        public void RestoreVersion3_PreservesExplicitRemainingInventoryAndStartsWithNoPlacements()
        {
            Restore(@"{""Version"":3,""LastSubmitDateIso"":""2026-08-11"",""Flowers"":[{""FlowerId"":""喜悦_angel_2026-08-10"",""DateIso"":""2026-08-10"",""WeekId"":202633,""EmotionType"":""喜悦"",""FlowerName"":""日轮"",""EmotionDetail"":""开心"",""Owner"":""angel"",""State"":1,""IsCollected"":true,""CreatedAtUtcTicks"":0}],""Clusters"":[{""EmotionType"":""喜悦"",""Owner"":""angel"",""TotalCount"":1,""UnlockedStage"":1}],""PlacementInventories"":[{""EmotionType"":""喜悦"",""Owner"":""angel"",""SingleCount"":0,""ClusterCount"":0}]}");

            Assert.AreEqual(0, _service.GetPlacementInventory("喜悦", "angel").SingleCount,
                "v3 已保存的实际剩余库存不能在升级时按累计开花数重新补发。");
            Assert.AreEqual(0, _service.GetPlacedFlowers().Count);
        }

        [Test]
        public void TryPlaceFlower_FailureLeavesInventoryAndPlacementsUnchanged()
        {
            Restore(@"{""Version"":3,""Flowers"":[],""Clusters"":[],""PlacementInventories"":[{""EmotionType"":""喜悦"",""Owner"":""angel"",""SingleCount"":2,""ClusterCount"":0}]}");

            Assert.True(_service.TryPlaceFlower("喜悦", "angel", false, 0, 0f, 0f));
            Assert.False(_service.TryPlaceFlower("喜悦", "angel", false, 0, 4f, 4f));

            Assert.AreEqual(1, _service.GetPlacementInventory("喜悦", "angel").SingleCount);
            Assert.AreEqual(1, _service.GetPlacedFlowers().Count);
            Assert.AreEqual(0f, _service.GetPlacedFlowers()[0].WorldX);
        }

        [Test]
        public void SubmitEmotion_CreatesAndPersistsDailySummary()
        {
            EmotionFlowerData? flower = _service.SubmitEmotion(string.Empty, "今天有点累，但完成了一件重要的事", "angel");

            Assert.True(flower.HasValue);
            EmotionDailySummaryData? summary = _service.GetTodayDailySummary();
            Assert.True(summary.HasValue);
            Assert.AreEqual("2026-08-12", summary.Value.DateIso);
            StringAssert.Contains("今天有点累", summary.Value.InputSentence);
            Assert.IsNotEmpty(summary.Value.Summary);
            Assert.IsNotEmpty(summary.Value.AngelNote);
            Assert.IsNotEmpty(summary.Value.DevilNote);

            string captured = ((IPersistentService)_service).CaptureJson();
            Restore(captured);

            EmotionDailySummaryData? restored = _service.GetTodayDailySummary();
            Assert.True(restored.HasValue);
            Assert.AreEqual(summary.Value.Summary, restored.Value.Summary);
            Assert.AreEqual(summary.Value.AngelNote, restored.Value.AngelNote);
            Assert.AreEqual(summary.Value.DevilNote, restored.Value.DevilNote);
        }

        [Test]
        public void CaptureAndRestore_RoundTripsClusterPlacement()
        {
            Restore(@"{""Version"":3,""Flowers"":[],""Clusters"":[],""PlacementInventories"":[{""EmotionType"":""平静"",""Owner"":""demon"",""SingleCount"":0,""ClusterCount"":1}]}");

            Assert.True(_service.TryPlaceFlower("平静", "demon", true, 7, -3.5f, 2f));
            string captured = ((IPersistentService)_service).CaptureJson();
            Restore(captured);

            PlacementFlowerInventory inventory = _service.GetPlacementInventory("平静", "demon");
            Assert.AreEqual(0, inventory.ClusterCount);
            Assert.AreEqual(1, _service.GetPlacedFlowers().Count);
            Assert.True(_service.GetPlacedFlowers()[0].IsCluster);
            Assert.AreEqual(7, _service.GetPlacedFlowers()[0].SlotIndex);
        }

        private void Restore(string json)
        {
            Assert.True(((IPersistentService)_service).RestoreJson(json));
        }

        private sealed class FixedClock : IGameClock
        {
            public DateTime Now => new(2026, 8, 12, 12, 0, 0, DateTimeKind.Local);
            public DateTime UtcNow => new(2026, 8, 12, 4, 0, 0, DateTimeKind.Utc);
            public string TodayIso => "2026-08-12";
            public bool IsToday(string isoDate) => string.Equals(isoDate, TodayIso, StringComparison.Ordinal);
            public TimeSpan ElapsedSinceUtc(DateTime utcWhen) => UtcNow - utcWhen;
            public void DebugAdvanceDays(int days) { }
            public void DebugResetClock() { }
        }
    }
}
