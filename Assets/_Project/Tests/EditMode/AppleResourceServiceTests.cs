#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Apple;
using GeminiLab.Modules.EmotionGarden;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class AppleResourceServiceTests
    {
        private FakeGameClock _clock = null!;
        private AppleService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeGameClock
            {
                Now = new DateTime(2026, 8, 14, 8, 0, 0, DateTimeKind.Local),
                UtcNow = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            _service = new AppleService(_clock, new EventBus(), randomSeed: 17);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Reset();
        }

        [Test]
        public void NewService_StartsWithTwentyApples()
        {
            Assert.AreEqual(20, _service.Balance);
        }

        [Test]
        public void TreeGeneration_UsesFortyFiveToNinetyMinuteRoundsAndSurvivesRestore()
        {
            _service.EnsureTree("world_tree_1");
            _clock.Advance(TimeSpan.FromMinutes(91));

            int generated = _service.GetPendingCount("world_tree_1");
            Assert.That(generated, Is.InRange(1, 2));
            AppleTreeState state = _service.GetTreeStates()[0];
            long intervalMinutes = (state.NextGenerationUtcTicks - state.LastGeneratedUtcTicks) / TimeSpan.TicksPerMinute;
            Assert.That(intervalMinutes, Is.InRange(45, 90));
            string saved = _service.CaptureJson();

            var restored = new AppleService(_clock, new EventBus(), randomSeed: 17);
            Assert.IsTrue(restored.RestoreJson(saved));
            Assert.AreEqual(generated, restored.GetPendingCount("world_tree_1"));
            Assert.AreEqual(generated, restored.ShakeTree("world_tree_1"));
            Assert.AreEqual(20 + generated, restored.Balance);
            Assert.AreEqual(0, restored.ShakeTree("world_tree_1"));
        }

        [Test]
        public void TreeGeneration_IsCappedAtFiveRoundsPerDayAndUnclaimedApplesAccumulate()
        {
            _service.EnsureTree("world_tree_3");
            _clock.Advance(TimeSpan.FromDays(2).Add(TimeSpan.FromHours(18)));

            int pending = _service.GetPendingCount("world_tree_3");
            AppleTreeState state = _service.GetTreeStates()[0];
            Assert.AreEqual(5, state.GeneratedRoundsToday);
            Assert.That(pending, Is.GreaterThanOrEqualTo(5));
            Assert.AreEqual(20, _service.Balance);
            Assert.AreEqual(pending, _service.ShakeTree("world_tree_3"));
            Assert.AreEqual(20 + pending, _service.Balance);
            Assert.AreEqual(0, _service.ShakeTree("world_tree_3"));
        }

        [Test]
        public void SpendRejectsInsufficientBalanceAndDoesNotGoNegative()
        {
            Assert.IsFalse(_service.TrySpend(21));
            Assert.AreEqual(20, _service.Balance);
            Assert.IsTrue(_service.TrySpend(5));
            Assert.AreEqual(15, _service.Balance);
        }

        [Test]
        public void BloomingAFlowerRewardsTwelveApplesOnlyOnce()
        {
            ServiceLocator.Register<IAppleService>(_service);
            var eventBus = new EventBus();
            var host = new GameObject("AppleRewardGardenTest");
            try
            {
                var garden = host.AddComponent<EmotionGardenService>();
                garden.Initialize(_clock, eventBus);

                EmotionFlowerData? flower = garden.SubmitEmotion("喜悦", "开心", "angel");
                Assert.IsTrue(flower.HasValue);
                Assert.IsTrue(garden.SetBloomed(flower!.Value.FlowerId));
                Assert.AreEqual(32, _service.Balance);
                Assert.IsFalse(garden.SetBloomed(flower.Value.FlowerId));
                Assert.AreEqual(32, _service.Balance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ConsumerCosts_MatchAppleEconomyRequirements()
        {
            Assert.AreEqual(20, GeminiLab.Modules.Collection.GachaService.SingleCost);
            Assert.AreEqual(100, GeminiLab.Modules.Collection.GachaService.MultiCost);
            Assert.AreEqual(8, GeminiLab.Modules.Tarot.TarotService.DefaultSessionCost);
        }
    }
}
