#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.UI;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Apple;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using GeminiLab;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗系统运行态宿主。挂在 Boot.unity 的 BootstrapRoot 上；DontDestroyOnLoad。
    /// 在 Inspector 拖入 `TarotDeckSO` 和可选的 `LLMConfigSO`。
    /// LLMConfigSO 已配置 API Key → 使用 DirectLLMBackend；
    /// 否则 Gateway 可用 → GatewayTarotBackend；
    /// 否则 → FallbackOnlyBackend（全本地解读）。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TarotRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private TarotDeckSO? _deck;
        [SerializeField] private LLMConfigSO? _llmConfig;

        private IDisposable? _drawnSub;
        private EventBus? _eventBus;

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {

            if (_deck == null)
            {
                Debug.LogError("[TarotBootstrap] 未绑定 TarotDeckSO，塔罗服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out _eventBus);

            if (!ServiceLocator.TryResolve(out IAppleService? apple) || apple is null)
            {
                Debug.LogError("[TarotBootstrap] IAppleService 未注册，请先挂 AppleRuntimeBootstrap");
                return;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                Debug.LogError("[TarotBootstrap] IGameClock 未注册，塔罗服务无法初始化");
                return;
            }

            ITarotReadingBackend backend = ResolveBackend();
            var service = new TarotService(_deck, apple, clock, _eventBus, backend);
            ServiceLocator.Register<ITarotService>(service);

            // Register session record store for persistence
            var recordStore = new TarotSessionRecordStore();
            ServiceLocator.Register<ITarotSessionRecordStore>(recordStore);
            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
                registry.Register(recordStore);

            if (DebugDisplaySettingsSO.Instance.IsVerboseLoggingEnabled)
                Debug.Log($"[TarotBootstrap] TarotService registered. Backend: {backend.GetType().Name}");

            if (_eventBus is not null)
            {
                _drawnSub = _eventBus.Subscribe<TarotDrawnEvent>(OnTarotDrawn);
            }
        }

        private ITarotReadingBackend ResolveBackend()
        {
            if (_llmConfig != null && _llmConfig.IsConfigured)
            {
                if (DebugDisplaySettingsSO.Instance.IsVerboseLoggingEnabled)
                    Debug.Log("[TarotBootstrap] 使用 DirectLLMBackend");
                return new DirectLLMBackend(_llmConfig, ResolvePersonalityText);
            }

            if (ServiceLocator.TryResolve(out IGatewayClient? client) && client is not null)
            {
                if (DebugDisplaySettingsSO.Instance.IsVerboseLoggingEnabled)
                    Debug.Log("[TarotBootstrap] 使用 GatewayTarotBackend");
                return new GatewayTarotBackend(client);
            }

            if (DebugDisplaySettingsSO.Instance.IsVerboseLoggingEnabled)
                Debug.Log("[TarotBootstrap] 使用 FallbackOnlyBackend（本地解读）");
            return new FallbackOnlyBackend();
        }

        private void OnDestroy()
        {
            _drawnSub?.Dispose();
        }

        private static string ResolvePersonalityText(PetId petId)
        {
            if (ServiceLocator.TryResolve(out IPersonalityEvolutionService? evo) && evo is not null)
            {
                var pv = evo.GetMatrix(petId);
                return $"善良:{pv.Kindness:F1} 邪恶:{pv.Evilness:F1} 沉着:{pv.Calmness:F1} " +
                       $"勇敢:{pv.Bravery:F1} 害羞:{pv.Shyness:F1} 正直:{pv.Integrity:F1} 好奇:{pv.Curiosity:F1}";
            }
            return "性格数据未加载";
        }

        private void OnTarotDrawn(TarotDrawnEvent evt)
        {
        }

        private sealed class FallbackOnlyBackend : ITarotReadingBackend
        {
            public System.Threading.Tasks.Task<TarotReading> RequestAsync(
                TarotDrawResult draw,
                GeminiLab.Modules.Pet.PetId petId,
                TarotOrientation orientation,
                System.Threading.CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(LocalFallback.Build(draw, petId, orientation));
            }

            public System.Threading.Tasks.Task<TarotSummaryResult> RequestSummaryAsync(
                TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
                string? question, System.Threading.CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(TarotSummaryResult.Default());
            }
        }
    }
}
