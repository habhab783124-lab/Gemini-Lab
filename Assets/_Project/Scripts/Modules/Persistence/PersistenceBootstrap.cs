#nullable enable
using System;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.ApartmentKeepsake;
using GeminiLab.Modules.EmotionGarden;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// 在场景加载完成后补齐 SaveSystem + SaveCoordinator。
    /// 用 AfterSceneLoad 是为了等其他业务 Bootstrap（Settings / Inventory / Collection / Tarot）
    /// 完成自身 <see cref="IPersistentServiceRegistry"/> 注册；Coordinator 只在调用时实时查 Registry，
    /// 所以顺序只影响"第一次 List / Save"能看到哪些服务。
    /// </summary>
    public static class PersistenceBootstrap
    {
        private const string AutoSlot = "autosave";
        private static IDisposable? s_emotionSubmittedSubscription;
        private static IDisposable? s_keepsakeChangedSubscription;
        private static bool s_saveInProgress;
        private static bool s_saveQueued;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_emotionSubmittedSubscription?.Dispose();
            s_keepsakeChangedSubscription?.Dispose();
            s_emotionSubmittedSubscription = null;
            s_keepsakeChangedSubscription = null;
            s_saveInProgress = false;
            s_saveQueued = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (!ServiceLocator.TryResolve(out ISaveSystem? saveSystem) || saveSystem is null)
            {
                // 开发者模式与玩家模式的存档完全隔离：
                // 调试数据（时钟快进产生的未来日期等）不会污染真实进度，反之亦然
                string saveRoot = System.IO.Path.Combine(
                    Application.persistentDataPath, DevMode.Active ? "Saves-Dev" : "Saves");
                saveSystem = new SaveSystem(saveRootPath: saveRoot);
                ServiceLocator.Register(saveSystem);
                Debug.Log($"[PersistenceBootstrap] SaveSystem registered. 存档目录: {(DevMode.Active ? "Saves-Dev (开发者)" : "Saves (玩家)")}");
            }

            if (ServiceLocator.TryResolve(out ISaveCoordinator? _))
            {
                if (ServiceLocator.TryResolve(out ISaveCoordinator? existingCoordinator) && existingCoordinator is not null)
                {
                    EnsureAutosaveSubscriptions(existingCoordinator);
                }

                return;
            }

            if (!ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) || registry is null)
            {
                Debug.LogError("[PersistenceBootstrap] IPersistentServiceRegistry 未注册，Coordinator 无法初始化");
                return;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                Debug.LogError("[PersistenceBootstrap] IGameClock 未注册，Coordinator 无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);

            var coordinator = new SaveCoordinator(saveSystem, registry, clock, eventBus);
            ServiceLocator.Register<ISaveCoordinator>(coordinator);
            EnsureAutosaveSubscriptions(coordinator);
            Debug.Log("[PersistenceBootstrap] SaveCoordinator registered.");

            // 创建自动存档/读档管理器
            var autoSaveGo = new GameObject("AutoSaveManager");
            UnityEngine.Object.DontDestroyOnLoad(autoSaveGo);
            autoSaveGo.AddComponent<AutoSaveManager>();
            Debug.Log("[PersistenceBootstrap] AutoSaveManager created.");
        }

        internal static void EnsureAutosaveSubscriptions(ISaveCoordinator coordinator)
        {
            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus is null)
            {
                return;
            }

            if (s_emotionSubmittedSubscription is null)
            {
                s_emotionSubmittedSubscription = eventBus.Subscribe<EmotionFlowerSubmittedEvent>(
                    _ => QueueAutosave(coordinator));
                Debug.Log("[PersistenceBootstrap] 已绑定情绪提交 autosave。");
            }

            if (s_keepsakeChangedSubscription is null)
            {
                s_keepsakeChangedSubscription = eventBus.Subscribe<ApartmentKeepsakeStateChangedEvent>(evt =>
                {
                    if (evt.RequestAutosave)
                    {
                        QueueAutosave(coordinator);
                    }
                });
                Debug.Log("[PersistenceBootstrap] 已绑定遗留物状态 autosave。");
            }
        }

        // 保留旧入口，避免已有调试调用失效。
        internal static void EnsureEmotionAutosaveSubscription(ISaveCoordinator coordinator)
        {
            EnsureAutosaveSubscriptions(coordinator);
        }

        private static void QueueAutosave(ISaveCoordinator coordinator)
        {
            if (s_saveInProgress)
            {
                s_saveQueued = true;
                return;
            }

            s_saveInProgress = true;
            _ = FlushAutosaveAsync(coordinator);
        }

        private static async Task FlushAutosaveAsync(ISaveCoordinator coordinator)
        {
            try
            {
                do
                {
                    s_saveQueued = false;
                    await coordinator.SaveAsync(AutoSlot);
                }
                while (s_saveQueued);

                Debug.Log("[PersistenceBootstrap] 即时 autosave 已完成。");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceBootstrap] 即时 autosave 失败: {exception.Message}");
            }
            finally
            {
                s_saveInProgress = false;
                if (s_saveQueued)
                {
                    QueueAutosave(coordinator);
                }
            }
        }
    }
}
