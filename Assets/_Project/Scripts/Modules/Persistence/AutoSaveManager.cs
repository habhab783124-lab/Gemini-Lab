#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>首次 autosave 读取尝试结束；无存档也会发布，供每日玩法安全开始判定。</summary>
    public readonly struct AutoSaveInitialLoadFinishedEvent
    {
        public AutoSaveInitialLoadFinishedEvent(bool loaded)
        {
            Loaded = loaded;
        }

        public bool Loaded { get; }
    }

    /// <summary>
    /// 自动存档/读档管理器。退出时自动保存到 "autosave"，启动时自动恢复。
    /// 由 PersistenceBootstrap 创建，DontDestroyOnLoad。
    /// </summary>
    public sealed class AutoSaveManager : MonoBehaviour
    {
        private const string AutoSlot = "autosave";

        public static bool InitialLoadCompleted { get; private set; }
        public static bool InitialLoadSucceeded { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoadState()
        {
            InitialLoadCompleted = false;
            InitialLoadSucceeded = false;
        }

        private async void Start()
        {
            // 等待一帧，确保所有模块 Bootstrap 完成 IPersistentService 注册
            await System.Threading.Tasks.Task.Yield();

            if (!ServiceLocator.TryResolve(out ISaveCoordinator? coordinator) || coordinator is null)
            {
                Debug.LogWarning("[AutoSave] ISaveCoordinator 未注册，跳过自动读档");
                CompleteInitialLoad(false);
                return;
            }

            // AfterSceneLoad 早于部分业务 Bootstrap；此时补绑所有需要即时落盘的业务事件。
            PersistenceBootstrap.EnsureAutosaveSubscriptions(coordinator);

            bool loaded = false;
            try
            {
                loaded = await coordinator.LoadAsync(AutoSlot);
                if (loaded)
                    Debug.Log("[AutoSave] 自动读档成功");
                else
                    Debug.Log("[AutoSave] 未找到 autosave 存档，使用全新数据");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AutoSave] 自动读档失败，使用当前内存状态: {exception.Message}");
            }
            finally
            {
                CompleteInitialLoad(loaded);
            }
        }

        private static void CompleteInitialLoad(bool loaded)
        {
            InitialLoadSucceeded = loaded;
            InitialLoadCompleted = true;
            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                eventBus.Publish(new AutoSaveInitialLoadFinishedEvent(loaded));
            }
        }

        private async void OnApplicationQuit()
        {
            if (!ServiceLocator.TryResolve(out ISaveCoordinator? coordinator) || coordinator is null)
            {
                Debug.LogWarning("[AutoSave] ISaveCoordinator 未注册，跳过自动存档");
                return;
            }

            await coordinator.SaveAsync(AutoSlot);
            Debug.Log("[AutoSave] 自动存档成功");
        }
    }
}
