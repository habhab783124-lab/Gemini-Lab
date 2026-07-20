using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopOverlaySceneController : MonoBehaviour
    {
        private static string previousSceneName;

        public static void EnterOverlay()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            previousSceneName = currentScene.name;

            PlayerPrefs.SetString("DesktopOverlay_PreviousScene", previousSceneName);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Desktop_Overlay", LoadSceneMode.Single);
        }

        public static void ExitOverlay()
        {
            string sceneName = PlayerPrefs.GetString("DesktopOverlay_PreviousScene", "MainMenu");

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
