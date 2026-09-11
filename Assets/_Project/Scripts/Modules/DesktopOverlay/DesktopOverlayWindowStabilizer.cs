using System.Collections;
using UnityEngine;
using Kirurobo;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopOverlayWindowStabilizer : MonoBehaviour
    {
        private IEnumerator Start()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // 等 UniWindowController 完成第一次原生窗口初始化
            yield return null;
            yield return null;

            UniWindowController window = UniWindowController.current;

            if (window == null)
            {
                Debug.LogWarning(
                    "[DesktopOverlay] UniWindowController.current is null."
                );
                yield break;
            }

            // 让插件先执行 Fit Monitor
            window.shouldFitMonitor = true;

            // 等待窗口真正切到显示器尺寸
            for (int i = 0; i < 30; i++)
            {
                yield return null;

                bool sizeReady =
                    Screen.width == Display.main.systemWidth &&
                    Screen.height == Display.main.systemHeight;

                if (sizeReady)
                    break;
            }

            // 关键修复：
            // Fit Monitor 有时只改尺寸，没有正确恢复窗口原点
            window.windowPosition = Vector2.zero;

            // 第二次进入以后日志显示 Topmost 会掉成 false，
            // 这里顺便恢复
            window.isTopmost = true;

            // 再等几帧，防止插件后续一步把位置覆盖回去
            for (int i = 0; i < 5; i++)
            {
                yield return null;

                window.windowPosition = Vector2.zero;
                window.isTopmost = true;
            }

            Debug.Log(
                $"[DesktopOverlay] Window stabilized | " +
                $"Position={window.windowPosition} | " +
                $"Size={window.windowSize} | " +
                $"Topmost={window.isTopmost}"
            );
#else

    // Editor 中不执行窗口修复，但必须让 IEnumerator 正常结束
    yield break;
#endif
        }
    }
}
