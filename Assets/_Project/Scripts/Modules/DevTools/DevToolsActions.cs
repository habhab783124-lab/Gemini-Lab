#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Time;
using GeminiLab.Modules.EmotionGarden;
using UnityEngine;

namespace GeminiLab.Modules.DevTools
{
    /// <summary>
    /// DevTools 面板按钮响应。放在 DevTools GameObject 上，由 Editor 补丁绑定。
    /// </summary>
    public sealed class DevToolsActions : MonoBehaviour
    {
        private void Awake()
        {
            // 玩家模式（含打包版本）下整组调试工具隐藏
            if (!DevMode.Active) gameObject.SetActive(false);
        }

        public void AdvanceDay()
        {
            if (!DevMode.Active) return;

            if (ServiceLocator.TryResolve(out IGameClock? clock) && clock != null)
                clock.DebugAdvanceDays(1);

            if (ServiceLocator.TryResolve(out IDailyResetService? dailyReset) && dailyReset != null)
                dailyReset.CheckAndReset();

            if (ServiceLocator.TryResolve(out IEmotionGardenService? service) && service != null)
                service.RefreshBlooming();

            Debug.Log($"[DevTools] 已进入 {clock?.TodayIso ?? "?"}");
        }

        public void ResetClock()
        {
            if (!DevMode.Active) return;

            if (ServiceLocator.TryResolve(out IGameClock? clock) && clock != null)
                clock.DebugResetClock();

            if (ServiceLocator.TryResolve(out IEmotionGardenService? service) && service != null)
                service.RefreshBlooming();

            Debug.Log($"[DevTools] 时钟已重置，当前 {clock?.TodayIso ?? "?"}");
        }

        public void ClearGardenData()
        {
            if (!DevMode.Active) return;

            if (ServiceLocator.TryResolve(out IEmotionGardenService? service) && service != null)
                service.ClearAllData();

            Debug.Log("[DevTools] 花园数据已清空");
        }
    }
}
