#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.EmotionGarden;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class EmotionGardenAiTests
    {
        private GameObject _host = null!;
        private EmotionGardenService _service = null!;
        private MutableClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();
            _host = new GameObject("EmotionGardenAiTests");
            _service = _host.AddComponent<EmotionGardenService>();
            _clock = new MutableClock(new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Local));
            _service.Initialize(_clock, new EventBus());
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Reset();
            UnityEngine.Object.DestroyImmediate(_host);
        }

        [Test]
        public void SubmitEmotionAsync_StoresAiFlowerFieldsAndDailySummary()
        {
            const string summary = "今天把忙乱的心情写下来，也为自己留出了一点喘息空间，新的花朵替你收好这份温柔。";
            const string angelNote = "天使看见你认真记录今天的感受，并把这份勇气变成花语，愿你在平静里继续向前，也相信明天会有新的可能。";
            const string devilNote = "恶魔记下你没有逃避今天的情绪，先承认它，再带着这点清醒去做下一件真正重要的事，别停在原地。";
            var aiResult = new EmotionGardenAiResult
            {
                EmotionType = "喜悦",
                EmotionKeywords = new[] { "轻松", "期待", "明亮" },
                FlowerDescription = "一朵把轻快心情收进花瓣的日轮花。",
                FlowerLanguage = "日轮花的花语是：带着明亮心意继续前行。",
                Summary = summary,
                AngelNote = angelNote,
                DevilNote = devilNote
            };
            ServiceLocator.Register<IEmotionGardenAiProvider>(new FakeProvider(aiResult));

            EmotionFlowerData? submitted = _service.SubmitEmotionAsync(
                string.Empty,
                "今天特别开心，完成了重要的事情，心里很轻松。",
                EmotionFlowerCatalog.OwnerAngel).GetAwaiter().GetResult();

            Assert.True(submitted.HasValue);
            Assert.AreEqual("喜悦", submitted.Value.EmotionType);
            CollectionAssert.AreEqual(new[] { "轻松", "期待", "明亮" }, submitted.Value.EmotionKeywords);
            Assert.AreEqual(aiResult.FlowerDescription, submitted.Value.FlowerDescription);
            Assert.AreEqual(aiResult.FlowerLanguage, submitted.Value.FlowerLanguage);

            EmotionDailySummaryData? dailySummary = _service.GetTodayDailySummary();
            Assert.True(dailySummary.HasValue);
            Assert.AreEqual(summary, dailySummary.Value.Summary);
            Assert.AreEqual(angelNote, dailySummary.Value.AngelNote);
            Assert.AreEqual(devilNote, dailySummary.Value.DevilNote);
        }

        [Test]
        public void SubmitEmotionAsync_InvalidAiEmotionFallsBackToLocalClassification()
        {
            ServiceLocator.Register<IEmotionGardenAiProvider>(new FakeProvider(new EmotionGardenAiResult
            {
                EmotionType = "不存在的情绪",
                EmotionKeywords = Array.Empty<string>(),
                Summary = "太短",
                AngelNote = "太短",
                DevilNote = "太短"
            }));

            EmotionFlowerData? submitted = _service.SubmitEmotionAsync(
                string.Empty,
                "今天特别开心，完成了重要的事情，心里很轻松。",
                EmotionFlowerCatalog.OwnerDemon).GetAwaiter().GetResult();

            Assert.True(submitted.HasValue);
            Assert.AreEqual("喜悦", submitted.Value.EmotionType);
            Assert.Greater(submitted.Value.EmotionKeywords.Length, 0);
            StringAssert.Contains("今天记录了", _service.GetTodayDailySummary().Value.Summary);
        }

        private sealed class FakeProvider : IEmotionGardenAiProvider
        {
            private readonly EmotionGardenAiResult _result;

            public FakeProvider(EmotionGardenAiResult result)
            {
                _result = result;
            }

            public Task<EmotionGardenAiResult?> GenerateAsync(
                string dateIso,
                string inputSentence,
                string emotionType,
                string owner,
                string flowerName,
                string flowerDescription,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<EmotionGardenAiResult?>(_result.Clone());
            }
        }

        private sealed class MutableClock : IGameClock
        {
            private DateTime _localNow;

            public MutableClock(DateTime localNow)
            {
                _localNow = localNow;
            }

            public DateTime Now => _localNow;
            public DateTime UtcNow => _localNow.ToUniversalTime();
            public string TodayIso => _localNow.ToString("yyyy-MM-dd");
            public bool IsToday(string isoDate) => string.Equals(isoDate, TodayIso, StringComparison.Ordinal);
            public TimeSpan ElapsedSinceUtc(DateTime utcWhen) => UtcNow - utcWhen;
            public void DebugAdvanceDays(int days) => _localNow = _localNow.AddDays(days);
            public void DebugResetClock() { }
        }
    }
}
