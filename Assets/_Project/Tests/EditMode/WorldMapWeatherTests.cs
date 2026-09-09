#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.WorldMap;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class WorldMapWeatherTests
    {
        [TestCase(0, WorldMapWeatherKind.Sunny)]
        [TestCase(3, WorldMapWeatherKind.Sunny)]
        [TestCase(45, WorldMapWeatherKind.Sunny)]
        [TestCase(51, WorldMapWeatherKind.Rainy)]
        [TestCase(67, WorldMapWeatherKind.Rainy)]
        [TestCase(80, WorldMapWeatherKind.Rainy)]
        [TestCase(95, WorldMapWeatherKind.Rainy)]
        [TestCase(99, WorldMapWeatherKind.Rainy)]
        public void Classifier_MapsOpenMeteoCodes(int code, WorldMapWeatherKind expected)
        {
            Assert.That(WorldMapWeatherClassifier.Classify(code), Is.EqualTo(expected));
        }

        [Test]
        public void OpenMeteoParser_ReadsCurrentWeatherCodeAndTime()
        {
            WeatherSnapshot snapshot = OpenMeteoWeatherProvider.ParseSnapshot(
                "{\"current\":{\"time\":\"2026-08-18T09:30\",\"weather_code\":61}}",
                new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(snapshot.Kind, Is.EqualTo(WorldMapWeatherKind.Rainy));
            Assert.That(snapshot.WeatherCode, Is.EqualTo(61));
            Assert.That(snapshot.ObservedAtUtc, Is.EqualTo(new DateTime(2026, 8, 18, 9, 30, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void Service_CachesSuccessfulReadingAndSchedulesNextRefresh()
        {
            var provider = new FakeWeatherProvider(new WeatherSnapshot(
                WorldMapWeatherKind.Rainy,
                80,
                DateTime.UtcNow));
            var service = new WorldMapWeatherService(provider, TimeSpan.FromMinutes(30));
            DateTime now = new(2026, 8, 18, 1, 0, 0, DateTimeKind.Utc);

            WeatherSnapshot snapshot = service.RefreshCurrentAsync(31.2, 121.4, "auto", now)
                .GetAwaiter()
                .GetResult();

            Assert.That(snapshot.Kind, Is.EqualTo(WorldMapWeatherKind.Rainy));
            Assert.That(service.HasSuccessfulReading, Is.True);
            Assert.That(service.IsRefreshDue(now.AddMinutes(29)), Is.False);
            Assert.That(service.IsRefreshDue(now.AddMinutes(30)), Is.True);
            Assert.That(service.Current.WeatherCode, Is.EqualTo(80));
        }

        [Test]
        public void Service_FirstFailureFallsBackToSunnyAndKeepsError()
        {
            var service = new WorldMapWeatherService(
                new FakeWeatherProvider(new InvalidOperationException("offline")),
                TimeSpan.FromMinutes(30));

            WeatherSnapshot snapshot = service.RefreshCurrentAsync(
                31.2,
                121.4,
                "auto",
                new DateTime(2026, 8, 18, 1, 0, 0, DateTimeKind.Utc))
                .GetAwaiter()
                .GetResult();

            Assert.That(snapshot.Kind, Is.EqualTo(WorldMapWeatherKind.Sunny));
            Assert.That(service.HasSuccessfulReading, Is.False);
            Assert.That(service.LastError, Does.Contain("offline"));
        }

        [Test]
        public void Service_FailureAfterSuccessKeepsLastSuccessfulWeather()
        {
            var provider = new SequencedWeatherProvider(
                new WeatherSnapshot(WorldMapWeatherKind.Rainy, 61, DateTime.UtcNow),
                new InvalidOperationException("timeout"));
            var service = new WorldMapWeatherService(provider, TimeSpan.FromMinutes(30));
            DateTime first = new(2026, 8, 18, 1, 0, 0, DateTimeKind.Utc);

            service.RefreshCurrentAsync(31.2, 121.4, "auto", first)
                .GetAwaiter()
                .GetResult();
            WeatherSnapshot second = service.RefreshCurrentAsync(31.2, 121.4, "auto", first.AddMinutes(30))
                .GetAwaiter()
                .GetResult();

            Assert.That(second.Kind, Is.EqualTo(WorldMapWeatherKind.Rainy));
            Assert.That(service.HasSuccessfulReading, Is.True);
            Assert.That(service.LastError, Does.Contain("timeout"));
        }

        private sealed class FakeWeatherProvider : IWeatherProvider
        {
            private readonly WeatherSnapshot? _snapshot;
            private readonly Exception? _exception;

            public FakeWeatherProvider(WeatherSnapshot snapshot) => _snapshot = snapshot;
            public FakeWeatherProvider(Exception exception) => _exception = exception;

            public Task<WeatherSnapshot> FetchCurrentAsync(
                double latitude,
                double longitude,
                string timezone,
                CancellationToken cancellationToken)
            {
                if (_exception is not null)
                {
                    return Task.FromException<WeatherSnapshot>(_exception);
                }

                return Task.FromResult(_snapshot!.Value);
            }
        }

        private sealed class SequencedWeatherProvider : IWeatherProvider
        {
            private readonly WeatherSnapshot _snapshot;
            private readonly Exception _exception;
            private int _calls;

            public SequencedWeatherProvider(WeatherSnapshot snapshot, Exception exception)
            {
                _snapshot = snapshot;
                _exception = exception;
            }

            public Task<WeatherSnapshot> FetchCurrentAsync(
                double latitude,
                double longitude,
                string timezone,
                CancellationToken cancellationToken)
            {
                _calls++;
                return _calls == 1
                    ? Task.FromResult(_snapshot)
                    : Task.FromException<WeatherSnapshot>(_exception);
            }
        }
    }
}
