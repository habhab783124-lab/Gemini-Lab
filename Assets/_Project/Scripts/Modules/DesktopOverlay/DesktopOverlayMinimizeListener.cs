using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GeminiLab.Modules.DesktopOverlay
{
    public sealed class DesktopOverlayMinimizeListener : MonoBehaviour
    {
        private const string OverlaySceneName = "Desktop_Overlay";

        private static DesktopOverlayMinimizeListener instance;

        private readonly object windowHandleLock = new object();

        private IntPtr windowHandle = IntPtr.Zero;
        private System.Threading.Timer minimizeTimer;

        private volatile bool overlayActive;
        private volatile bool isQuitting;

        // 0 = no request
        // 1 = request entering Desktop_Overlay on Unity main thread
        private int pendingEnterOverlay;

        // 0 = idle
        // 1 = SW_RESTORE has been requested and we are waiting for Windows
        //     to report that the player window is no longer minimized.
        private int restoreInProgress;

        private int currentProcessId;
        private Coroutine enterOverlayRoutine;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(
            IntPtr hWnd,
            int nCmdShow
        );

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId
        );

#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning(
                    "[DesktopOverlay] Duplicate MinimizeListener removed."
                );

                // Only remove the duplicate component. Never destroy the whole
                // DesktopOverlaySystem host and its other services.
                Destroy(this);
                return;
            }

            instance = this;

            Application.runInBackground = true;

            currentProcessId = Process.GetCurrentProcess().Id;

            overlayActive =
                SceneManager.GetActiveScene().name ==
                OverlaySceneName;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            SetWindowHandle(GetProcessMainWindow());

            // Win32 state is checked from a timer so detection does not depend
            // on Unity continuing to receive Update while the window is minimized.
            minimizeTimer =
                new System.Threading.Timer(
                    CheckMinimized,
                    null,
                    300,
                    100
                );

#endif

            Debug.Log(
                "[DesktopOverlay] MinimizeListener Ready"
            );
        }

        private void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            CaptureUnityWindow();

            overlayActive =
                SceneManager.GetActiveScene().name ==
                OverlaySceneName;

            if (
                Interlocked.Exchange(
                    ref pendingEnterOverlay,
                    0
                ) == 1
            )
            {
                if (
                    !overlayActive &&
                    enterOverlayRoutine == null
                )
                {
                    enterOverlayRoutine =
                        StartCoroutine(
                            EnterOverlayAfterWindowSettles()
                        );
                }
            }

#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        private IEnumerator EnterOverlayAfterWindowSettles()
        {
            // Let the restored native player window settle before changing
            // scenes. UniWindowController remains responsible for its own
            // Inspector-driven settings such as Should Fit Monitor.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);

            overlayActive =
                SceneManager.GetActiveScene().name ==
                OverlaySceneName;

            if (!isQuitting && !overlayActive)
            {
                Debug.Log(
                    "[DesktopOverlay] Minimize detected -> EnterOverlay"
                );

                DesktopOverlaySceneController.EnterOverlay();
            }

            enterOverlayRoutine = null;
        }

        private void CaptureUnityWindow()
        {
            IntPtr foreground =
                GetForegroundWindow();

            if (foreground == IntPtr.Zero)
                return;

            GetWindowThreadProcessId(
                foreground,
                out uint processId
            );

            // Only remember a foreground window that belongs to this Unity player.
            if (
                processId ==
                (uint)currentProcessId
            )
            {
                SetWindowHandle(foreground);
            }
        }

        private void CheckMinimized(object state)
        {
            if (isQuitting)
                return;

            if (overlayActive)
                return;

            IntPtr handle =
                GetValidPlayerWindow();

            if (handle == IntPtr.Zero)
                return;

            // A restore was already requested. Do not enter the overlay until
            // Windows confirms that the player is no longer minimized.
            if (
                Volatile.Read(
                    ref restoreInProgress
                ) == 1
            )
            {
                if (!IsIconic(handle))
                {
                    Interlocked.Exchange(
                        ref restoreInProgress,
                        0
                    );

                    Interlocked.Exchange(
                        ref pendingEnterOverlay,
                        1
                    );
                }

                return;
            }

            if (!IsIconic(handle))
                return;

            // Prevent the 100 ms timer from issuing repeated restore requests.
            if (
                Interlocked.CompareExchange(
                    ref restoreInProgress,
                    1,
                    0
                ) != 0
            )
            {
                return;
            }

            bool restored =
                ShowWindowAsync(
                    handle,
                    SW_RESTORE
                );

            if (!restored)
            {
                Interlocked.Exchange(
                    ref restoreInProgress,
                    0
                );
            }
        }

        private IntPtr GetValidPlayerWindow()
        {
            IntPtr handle =
                GetWindowHandle();

            if (
                handle != IntPtr.Zero &&
                IsWindow(handle)
            )
            {
                return handle;
            }

            handle =
                GetProcessMainWindow();

            if (
                handle != IntPtr.Zero &&
                IsWindow(handle)
            )
            {
                SetWindowHandle(handle);
                return handle;
            }

            return IntPtr.Zero;
        }

        private IntPtr GetWindowHandle()
        {
            lock (windowHandleLock)
            {
                return windowHandle;
            }
        }

        private void SetWindowHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return;

            lock (windowHandleLock)
            {
                windowHandle = handle;
            }
        }

        private static IntPtr GetProcessMainWindow()
        {
            using (
                Process process =
                    Process.GetCurrentProcess()
            )
            {
                process.Refresh();

                return process.MainWindowHandle;
            }
        }

#endif

        private void OnApplicationQuit()
        {
            isQuitting = true;

            minimizeTimer?.Dispose();
            minimizeTimer = null;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            isQuitting = true;

            minimizeTimer?.Dispose();
            minimizeTimer = null;

            instance = null;
        }
    }
}
