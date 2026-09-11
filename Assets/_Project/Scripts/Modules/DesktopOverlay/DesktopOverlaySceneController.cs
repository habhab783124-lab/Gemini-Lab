using UnityEngine;
using UnityEngine.SceneManagement;
using Kirurobo;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopOverlaySceneController : MonoBehaviour
    {
        private const string OverlaySceneName = "Desktop_Overlay";

        private const string PreviousSceneKey =
            "DesktopOverlay_PreviousScene";

        private const string PreviousWidthKey =
            "DesktopOverlay_PreviousWidth";

        private const string PreviousHeightKey =
            "DesktopOverlay_PreviousHeight";


        public static void EnterOverlay()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            // 防止已经在桌宠场景里还重复进入
            if (currentScene.name == OverlaySceneName)
                return;

            // 记录进入桌宠模式前所在的 Scene
            PlayerPrefs.SetString(
                PreviousSceneKey,
                currentScene.name
            );

            // 记录主项目原本的窗口大小
            PlayerPrefs.SetInt(
                PreviousWidthKey,
                Screen.width
            );

            PlayerPrefs.SetInt(
                PreviousHeightKey,
                Screen.height
            );

            PlayerPrefs.Save();

            SceneManager.LoadScene(
                OverlaySceneName,
                LoadSceneMode.Single
            );
        }


        public static void ExitOverlay()
        {
            string previousSceneName =
                PlayerPrefs.GetString(
                    PreviousSceneKey,
                    "MainMenu"
                );

            int previousWidth =
                PlayerPrefs.GetInt(
                    PreviousWidthKey,
                    1280
                );

            int previousHeight =
                PlayerPrefs.GetInt(
                    PreviousHeightKey,
                    720
                );

            // Desktop_Overlay 还没被卸载时
            // 先恢复普通 Windows 窗口
            RestoreNormalWindow();

            // 然后才回主项目 Scene
            SceneManager.LoadScene(
                previousSceneName,
                LoadSceneMode.Single
            );
        }


        private static void RestoreNormalWindow()
        {
        #if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

            UniWindowController window =
                UniWindowController.current;

            if (window != null)
            {
                // 先解除桌宠模式
                window.isHitTestEnabled = false;
                window.isClickThrough = false;

                window.shouldFitMonitor = false;
                window.isZoomed = false;

                window.isTopmost = false;

                window.alphaValue = 1f;
                window.isTransparent = false;

                // ★ 关键：
                // 把 UniWindowController 控制的原生窗口
                // 明确缩回主程序尺寸
                window.windowSize =
                    new Vector2(1280f, 720f);
            }

        #endif

        Screen.fullScreenMode =
            FullScreenMode.Windowed;

        Screen.SetResolution(
            1280,
            720,
            FullScreenMode.Windowed
        );
    }
    }
}
