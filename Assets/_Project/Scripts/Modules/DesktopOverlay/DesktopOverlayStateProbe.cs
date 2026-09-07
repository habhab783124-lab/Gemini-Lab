using System.Collections;
using UnityEngine;
using Kirurobo;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopOverlayStateProbe : MonoBehaviour
    {
        private static int enterCount;

        private Camera mainCamera;
        private Transform angel;
        private Transform devil;

        private IEnumerator Start()
        {
            int currentEnter = ++enterCount;

            mainCamera = Camera.main;

            GameObject angelObject =
                GameObject.Find("angel");

            GameObject devilObject =
                GameObject.Find("devil");

            if (angelObject != null)
                angel = angelObject.transform;

            if (devilObject != null)
                devil = devilObject.transform;


            // Scene 刚进来
            LogState(currentEnter, "START");


            yield return null;
            yield return null;

            yield return new WaitForSecondsRealtime(
                0.2f
            );

            // 等窗口 / Camera 初步稳定
            LogState(currentEnter, "0.2s");


            yield return new WaitForSecondsRealtime(
                0.6f
            );

            // 最终稳定状态
            LogState(currentEnter, "0.8s");
        }


        private void LogState(
            int count,
            string stage
        )
        {
            UniWindowController window =
                UniWindowController.current;


            string cameraInfo = "Camera=NULL";
            string screenPositionInfo = "";

            if (mainCamera != null)
            {
                if (angel != null)
                {
                    Vector3 p =
                        mainCamera.WorldToScreenPoint(angel.position);

                    screenPositionInfo +=
                        $"AngelScreen={p} | ";
                }

                if (devil != null)
                {
                    Vector3 p =
                        mainCamera.WorldToScreenPoint(devil.position);

                    screenPositionInfo +=
                        $"DevilScreen={p}";
                }
            }

            if (mainCamera != null)
            {
                cameraInfo =
                    $"CamPos={mainCamera.transform.position} | " +
                    $"Ortho={mainCamera.orthographicSize:F3} | " +
                    $"Aspect={mainCamera.aspect:F4} | " +
                    $"PixelRect={mainCamera.pixelRect}";
            }


            string windowInfo =
                "UniWindow=NULL";

            if (window != null)
            {
                windowInfo =
                    $"WindowPos={window.windowPosition} | " +
                    $"WindowSize={window.windowSize} | " +
                    $"Fit={window.shouldFitMonitor} | " +
                    $"Transparent={window.isTransparent} | " +
                    $"Topmost={window.isTopmost}";
            }


            string angelInfo =
                angel == null
                    ? "Angel=NULL"
                    : $"AngelPos={angel.position} | " +
                      $"AngelScale={angel.lossyScale}";


            string devilInfo =
                devil == null
                    ? "Devil=NULL"
                    : $"DevilPos={devil.position} | " +
                      $"DevilScale={devil.lossyScale}";


            Debug.Log(
                $"[OverlayProbe #{count} {stage}] | " +
                $"Screen={Screen.width}x{Screen.height} | " +
                $"Display={Display.main.systemWidth}x" +
                $"{Display.main.systemHeight} | " +
                $"{windowInfo} | " +
                $"{cameraInfo} | " +
                $"{angelInfo} | " +
                $"{devilInfo} | " +
                $"{screenPositionInfo}"
            );

        }
    }
}
