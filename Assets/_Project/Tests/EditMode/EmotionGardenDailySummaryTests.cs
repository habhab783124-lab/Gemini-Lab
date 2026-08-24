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
    public sealed class EmotionGardenDailySummaryTests
    {
        private GameObject _host = null!;
        private EmotionGardenService _service = null!;
        private MutableClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("EmotionGardenDailySummaryTests");
            _service = _host.AddComponent<EmotionGardenService>();
            _clock = new MutableClock(new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Local));
            _service.Initialize(_clock, new EventBus());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
        }

        [Test]
        public void GetDailySummaryDates_ReturnsUniqueDatesDescending()
        {
            Assert.NotNull(_service.SubmitEmotion(string.Empty, "今天完成了一件重要的事", "angel"));

            _clock.SetDate(new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Local));
            Assert.NotNull(_service.SubmitEmotion(string.Empty, "今天也想慢慢来", "demon"));

            var dates = _service.GetDailySummaryDates();
            CollectionAssert.AreEqual(new[] { "2026-08-13", "2026-08-12" }, dates);
        }

        [Test]
        public void GetDailySummary_ReturnsSummaryForSelectedDate()
        {
            Assert.NotNull(_service.SubmitEmotion(string.Empty, "第一天的心情", "angel"));
            EmotionDailySummaryData? first = _service.GetDailySummary("2026-08-12");
            Assert.True(first.HasValue);

            _clock.SetDate(new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Local));
            Assert.NotNull(_service.SubmitEmotion(string.Empty, "第二天的心情", "demon"));
            EmotionDailySummaryData? second = _service.GetDailySummary("2026-08-13");

            Assert.True(second.HasValue);
            Assert.AreNotEqual(first.Value.Summary, second.Value.Summary);
            StringAssert.Contains("第二天", second.Value.InputSentence);
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

            public void SetDate(DateTime localNow)
            {
                _localNow = localNow;
            }
        }
    }
}
