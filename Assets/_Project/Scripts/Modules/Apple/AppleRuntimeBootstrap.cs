#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 注册唯一的 AppleService，并在时钟可用后建立当前场景的树状态。
    ///
    /// 不在 Awake 中强行注册：Boot 的 GameBootstrap 会在 Awake 中重置
    /// ServiceLocator。把注册放到 Start 和场景树的 Start，可保证核心时钟
    /// 已经存在，也保证调试按钮快进前树状态已经建立。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class AppleRuntimeBootstrap : MonoBehaviour
    {
        private static bool s_sceneLoadedHooked;
        private static IDisposable? s_newDaySubscription;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (s_sceneLoadedHooked)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            s_newDaySubscription?.Dispose();
            s_newDaySubscription = null;
            s_sceneLoadedHooked = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            HookSceneLoaded();
            InitializeCurrentScene();
        }

        private void Awake()
        {
            HookSceneLoaded();
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeCurrentScene();
        }

        /// <summary>
        /// Ensures that every caller uses the same service instance.
        /// </summary>
        public static bool EnsureRegistered()
        {
            if (ServiceLocator.TryResolve(out IAppleService? existing) && existing is not null)
            {
                RegisterForPersistence(existing);
                return true;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                return false;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);
            IAppleService service = new AppleService(clock, eventBus);
            ServiceLocator.Register(service);
            RegisterForPersistence(service);
            return true;
        }

        /// <summary>
        /// Initializes authored tree state before a player can use the debug
        /// clock controls. This method is safe to call repeatedly.
        /// </summary>
        public static bool InitializeCurrentScene()
        {
            if (!EnsureRegistered()) return false;
            HookNewDayEvent();
            if (!ServiceLocator.TryResolve(out IAppleService? service) || service is null)
            {
                return false;
            }

            AppleTreeInteractable[] trees = UnityEngine.Object.FindObjectsByType<AppleTreeInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int index = 0; index < trees.Length; index++)
            {
                AppleTreeInteractable tree = trees[index];
                if (tree == null || !tree.isActiveAndEnabled) continue;
                service.EnsureTree(tree.TreeId);
            }

            return true;
        }

        private static void HookNewDayEvent()
        {
            if (s_newDaySubscription != null) return;
            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus is null)
            {
                return;
            }

            s_newDaySubscription = eventBus.Subscribe<NewDayStartedEvent>(OnNewDayStarted);
        }

        private static void OnNewDayStarted(NewDayStartedEvent payload)
        {
            if (!InitializeCurrentScene()) return;
            if (!ServiceLocator.TryResolve(out IAppleService? service) || service is null)
            {
                Debug.LogWarning("[AppleRuntimeBootstrap][DayRefresh] 未找到 IAppleService");
                return;
            }

            AppleTreeInteractable[] trees = UnityEngine.Object.FindObjectsByType<AppleTreeInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            int refreshed = 0;
            for (int index = 0; index < trees.Length; index++)
            {
                AppleTreeInteractable tree = trees[index];
                if (tree == null || !tree.isActiveAndEnabled || string.IsNullOrWhiteSpace(tree.TreeId))
                {
                    continue;
                }

                int pending = service.GetPendingCount(tree.TreeId);
                refreshed++;
                Debug.Log(
                    $"[AppleRuntimeBootstrap][DayRefresh] date={payload.CurrentDateIso} " +
                    $"tree={tree.TreeId} pending={pending}",
                    tree);
            }

            Debug.Log($"[AppleRuntimeBootstrap][DayRefresh] refreshedTrees={refreshed}");
        }

        private static void HookSceneLoaded()
        {
            if (s_sceneLoadedHooked) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_sceneLoadedHooked = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeCurrentScene();
        }

        private static void RegisterForPersistence(IPersistentService service)
        {
            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(service);
            }
        }
    }
}
