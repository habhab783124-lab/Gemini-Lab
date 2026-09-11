#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    public static class DesktopOverlayRuntimeBootstrap
    {
        private const string HostName = "DesktopOverlaySystem";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureDesktopServices()
        {
            GameObject host = FindOrCreateHost();

            Object.DontDestroyOnLoad(host);

            EnsureComponent<DesktopOverlayManager>(host);
            EnsureComponent<ForegroundWindowProbe>(host);
            EnsureComponent<AppContextRuleEngine>(host);
            EnsureComponent<DesktopOverlayMinimizeListener>(host);
        }

        private static GameObject FindOrCreateHost()
        {
            // Prefer an already existing runtime service host if the project
            // scene has one, instead of creating a second DesktopOverlaySystem.
            DesktopOverlayManager? manager =
                Object.FindFirstObjectByType<DesktopOverlayManager>();

            if (manager is not null)
            {
                return manager.gameObject;
            }

            DesktopOverlayMinimizeListener? listener =
                Object.FindFirstObjectByType<DesktopOverlayMinimizeListener>();

            if (listener is not null)
            {
                return listener.gameObject;
            }

            GameObject? namedHost =
                GameObject.Find(HostName);

            if (namedHost is not null)
            {
                return namedHost;
            }

            return new GameObject(HostName);
        }

        private static T EnsureComponent<T>(
            GameObject host
        )
            where T : Component
        {
            T? component =
                host.GetComponent<T>();

            if (component is null)
            {
                component =
                    host.AddComponent<T>();
            }

            return component;
        }
    }
}
