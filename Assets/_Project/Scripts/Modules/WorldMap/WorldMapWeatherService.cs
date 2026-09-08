#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 天气读取、最近成功结果缓存和刷新节流。
    /// 服务本身不触碰 Sprite；场景控制器只消费 Current/WeatherChanged。
    /// </summary>
    public sealed class WorldMapWeatherService
    {
        private readonly IWeatherProvider _provider;
        private readonly TimeSpan _refreshInterval;
        private WeatherSnapshot _current = new(WorldMapWeatherKind.Sunny, 0, DateTime.MinValue);
        private bool _hasSuccessfulReading;
        private bool _isRefreshing;

        public WorldMapWeatherService(IWeatherProvider provider, TimeSpan refreshInterval)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _refreshInterval = refreshInterval > TimeSpan.Zero ? refreshInterval : TimeSpan.FromMinutes(30);
            NextRefreshUtc = DateTime.MinValue;
        }

        public WeatherSnapshot Current => _current;
        public bool HasSuccessfulReading => _hasSuccessfulReading;
        public bool IsRefreshing => _isRefreshing;
        public DateTime NextRefreshUtc { get; private set; }
        public string? LastError { get; private set; }
        public event Action<WeatherSnapshot>? WeatherChanged;

        public bool IsRefreshDue(DateTime utcNow)
        {
            DateTime normalized = NormalizeUtc(utcNow);
            return !_hasSuccessfulReading || normalized >= NextRefreshUtc;
        }

        public async Task<WeatherSnapshot> RefreshCurrentAsync(
            double latitude,
            double longitude,
            string timezone,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            if (_isRefreshing)
            {
                return _current;
            }

            _isRefreshing = true;
            DateTime normalizedNow = NormalizeUtc(utcNow);
            try
            {
                WeatherSnapshot snapshot = await _provider.FetchCurrentAsync(
                    latitude,
                    longitude,
                    timezone,
                    cancellationToken);
                _current = snapshot;
                _hasSuccessfulReading = true;
                LastError = null;
                WeatherChanged?.Invoke(snapshot);
                return snapshot;
            }
            catch (OperationCanceledException)
            {
                LastError = "天气请求已取消。";
                return _current;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return _current;
            }
            finally
            {
                _isRefreshing = false;
                NextRefreshUtc = normalizedNow + _refreshInterval;
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
    }
}
