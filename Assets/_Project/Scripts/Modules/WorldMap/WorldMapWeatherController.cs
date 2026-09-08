#nullable enable
using System;
using System.Threading;
using GeminiLab.Core;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 天气视觉控制器。
    /// 晴天/雨天覆盖节点、Sprite、尺寸和层级均由 Scene 保存；运行时只切换 enabled。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapWeatherController : MonoBehaviour
    {
        [Header("天气覆盖（Scene 作者化）")]
        [SerializeField] private SpriteRenderer? _sunnyOverlay;
        [SerializeField] private SpriteRenderer? _rainOverlay;

        [Header("当地天气位置")]
        [Tooltip("首版默认使用上海作为可运行占位；拿到产品地点后请在 Inspector 替换。")]
        [SerializeField] private double _latitude = 31.2304d;
        [SerializeField] private double _longitude = 121.4737d;
        [Tooltip("Open-Meteo 时区参数；auto 会按坐标返回当地时间。")]
        [SerializeField] private string _timezone = "auto";
        [SerializeField] private bool _useRemoteWeather = true;

        [Header("刷新")]
        [Min(1f)]
        [SerializeField] private float _refreshIntervalMinutes = 30f;

        private IGameClock? _clock;
        private WorldMapWeatherService? _service;
        private CancellationTokenSource? _cancellation;
        private float _nextUpdateTime;
        private bool _hasLoggedMissingClock;
        private bool _hasLoggedError;

        private void Awake()
        {
            ApplyKind(WorldMapWeatherKind.Sunny);
        }

        private void OnEnable()
        {
            _nextUpdateTime = 0f;
            _cancellation = new CancellationTokenSource();
            _service = new WorldMapWeatherService(
                new OpenMeteoWeatherProvider(),
                TimeSpan.FromMinutes(Mathf.Max(1f, _refreshIntervalMinutes)));
            _service.WeatherChanged += OnWeatherChanged;
            ApplyKind(WorldMapWeatherKind.Sunny);
        }

        private void OnDisable()
        {
            if (_service is not null)
            {
                _service.WeatherChanged -= OnWeatherChanged;
            }

            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private void Update()
        {
            if (!_useRemoteWeather || Time.unscaledTime < _nextUpdateTime)
            {
                return;
            }

            _nextUpdateTime = Time.unscaledTime + 5f;
            if (!ServiceLocator.TryResolve(out _clock) || _clock is null)
            {
                if (!_hasLoggedMissingClock)
                {
                    Debug.LogWarning("[WorldMapWeather] 未找到 IGameClock，暂不请求天气；下一次更新会重试。", this);
                    _hasLoggedMissingClock = true;
                }

                return;
            }

            if (_service is null || !_service.IsRefreshDue(_clock.UtcNow))
            {
                return;
            }

            _ = RefreshAsync(_clock.UtcNow);
        }

        private async System.Threading.Tasks.Task RefreshAsync(DateTime utcNow)
        {
            if (_service is null || _service.IsRefreshing || _cancellation is null)
            {
                return;
            }

            WeatherSnapshot snapshot = await _service.RefreshCurrentAsync(
                _latitude,
                _longitude,
                _timezone,
                utcNow,
                _cancellation.Token);
            if (!_service.HasSuccessfulReading && !_hasLoggedError && !string.IsNullOrWhiteSpace(_service.LastError))
            {
                Debug.LogWarning($"[WorldMapWeather] 首次天气请求失败，回退 Sunny：{_service.LastError}", this);
                _hasLoggedError = true;
            }

            ApplyKind(snapshot.Kind);
        }

        private void OnWeatherChanged(WeatherSnapshot snapshot)
        {
            _hasLoggedError = false;
            ApplyKind(snapshot.Kind);
        }

        private void ApplyKind(WorldMapWeatherKind kind)
        {
            if (_sunnyOverlay is not null)
            {
                _sunnyOverlay.enabled = kind == WorldMapWeatherKind.Sunny;
            }

            if (_rainOverlay is not null)
            {
                _rainOverlay.enabled = kind == WorldMapWeatherKind.Rainy;
            }
        }
    }
}
