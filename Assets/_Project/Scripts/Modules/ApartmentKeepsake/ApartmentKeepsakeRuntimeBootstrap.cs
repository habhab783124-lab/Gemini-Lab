#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.ApartmentKeepsake
{
    /// <summary>
    /// 注册跨场景遗留物服务。每日进入与 Relation 阈值由 Apartment Presenter 在初次读档完成后驱动。
    /// </summary>
    public static class ApartmentKeepsakeRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            IApartmentKeepsakeService? service;
            if (!ServiceLocator.TryResolve(out service) || service is null)
            {
                if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
                {
                    Debug.LogError("[ApartmentKeepsakeBootstrap] IGameClock 未注册，遗留物服务无法初始化。");
                    return;
                }

                ServiceLocator.TryResolve(out EventBus? eventBus);
                service = new ApartmentKeepsakeService(clock, eventBus);
                ServiceLocator.Register(service);
                Debug.Log("[ApartmentKeepsakeBootstrap] ApartmentKeepsakeService 已注册。");
            }

            if (service is IPersistentService persistent &&
                ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) &&
                registry is not null &&
                registry.TryGet(persistent.Key) is null)
            {
                registry.Register(persistent);
                Debug.Log("[ApartmentKeepsakeBootstrap] 遗留物持久化服务已注册。");
            }
        }
    }
}
