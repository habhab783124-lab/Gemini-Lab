#nullable enable
using System;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>当前 WorldMap 天气的最小视觉分类。</summary>
    public enum WorldMapWeatherKind
    {
        Sunny = 0,
        Rainy = 1
    }

    /// <summary>天气提供器返回的当前天气快照。</summary>
    public readonly struct WeatherSnapshot
    {
        public WeatherSnapshot(WorldMapWeatherKind kind, int weatherCode, DateTime observedAtUtc)
        {
            Kind = kind;
            WeatherCode = weatherCode;
            ObservedAtUtc = observedAtUtc.Kind == DateTimeKind.Utc
                ? observedAtUtc
                : observedAtUtc.ToUniversalTime();
        }

        public WorldMapWeatherKind Kind { get; }
        public int WeatherCode { get; }
        public DateTime ObservedAtUtc { get; }
    }

    /// <summary>
    /// Open-Meteo 使用 WMO weather code。首版只区分是否有降雨/雷暴，
    /// 其余晴朗、多云、雾和雪等天气沿用 Sunny 视觉，待美术资源到位再细分。
    /// </summary>
    public static class WorldMapWeatherClassifier
    {
        public static WorldMapWeatherKind Classify(int weatherCode)
        {
            return IsRainy(weatherCode) ? WorldMapWeatherKind.Rainy : WorldMapWeatherKind.Sunny;
        }

        public static bool IsRainy(int weatherCode)
        {
            return (weatherCode >= 51 && weatherCode <= 67)
                || (weatherCode >= 80 && weatherCode <= 82)
                || (weatherCode >= 95 && weatherCode <= 99);
        }
    }
}
