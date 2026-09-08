#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 根据本地真实时间切换 WorldMap 的夜幕覆盖层。
    /// 夜幕对象和 Sprite 由场景作者化保存；运行时只负责按 IGameClock 更新启用状态。
    /// </summary>
    public sealed class WorldMapDayNightController : MonoBehaviour
    {
        [Header("夜幕覆盖")]
        [SerializeField] private SpriteRenderer? _nightOverlay;

        [Header("夜晚天空资源")]
        [SerializeField] private SpriteRenderer? _starsRenderer;

        [Header("本地时间区间")]
        [Tooltip("从该小时开始视为白天。")]
        [Range(0, 23)]
        [SerializeField] private int _dayStartHour = 6;

        [Tooltip("从该小时开始视为夜晚。")]
        [Range(0, 23)]
        [SerializeField] private int _nightStartHour = 18;

        [Tooltip("检查真实时间的间隔，单位为秒。")]
        [Min(0.1f)]
        [SerializeField] private float _refreshIntervalSeconds = 5f;

        private IGameClock? _clock;
        private float _nextRefreshTime;
        private bool _hasAppliedState;
        private bool _lastNightState;

        private void Awake()
        {
            if (_nightOverlay == null)
            {
                _nightOverlay = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            _nextRefreshTime = 0f;
            _hasAppliedState = false;
            ApplyFromClock();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + _refreshIntervalSeconds;
            ApplyFromClock();
        }

        private void ApplyFromClock()
        {
            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock == null)
            {
                // GameBootstrap may register core services after this component's Awake.
                // Retry from Update instead of falling back to a second time source.
                return;
            }

            _clock = clock;
            bool isNight = IsNight(_clock.Now);
            if (_hasAppliedState && isNight == _lastNightState)
            {
                return;
            }

            _lastNightState = isNight;
            _hasAppliedState = true;
            if (_nightOverlay != null)
            {
                _nightOverlay.enabled = isNight;
            }

            if (_starsRenderer != null)
            {
                _starsRenderer.enabled = isNight;
            }
        }

        public bool IsNight(DateTime localTime)
        {
            TimeSpan time = localTime.TimeOfDay;
            TimeSpan dayStart = TimeSpan.FromHours(_dayStartHour);
            TimeSpan nightStart = TimeSpan.FromHours(_nightStartHour);

            if (_dayStartHour < _nightStartHour)
            {
                return time < dayStart || time >= nightStart;
            }

            // Supports an Inspector configuration whose daytime interval crosses midnight.
            return time >= nightStart && time < dayStart;
        }
    }
}
