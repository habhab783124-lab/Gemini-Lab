#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Registers navigation service during startup.
    /// </summary>
    public static class NavigationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterNavigation()
        {
            if (ServiceLocator.TryResolve(out INavigationService? _))
            {
                return;
            }

            ServiceLocator.Register<INavigationService>(new NavigationService());
            Debug.Log("[NavigationBootstrap] NavigationService registered.");
        }
    }
}
