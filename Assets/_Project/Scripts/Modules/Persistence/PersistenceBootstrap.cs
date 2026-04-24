#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// Registers persistence service after core bootstrap becomes available.
    /// </summary>
    public static class PersistenceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterPersistence()
        {
            if (ServiceLocator.TryResolve(out ISaveSystem? _))
            {
                return;
            }

            ServiceLocator.Register<ISaveSystem>(new SaveSystem());
            Debug.Log("[PersistenceBootstrap] SaveSystem registered.");
        }
    }
}
