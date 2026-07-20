using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopOverlayExitHotkey : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DesktopOverlaySceneController.ExitOverlay();
            }
        }
    }
}
