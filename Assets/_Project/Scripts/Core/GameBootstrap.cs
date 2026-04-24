#nullable enable
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Core
{
    /// <summary>
    /// Startup entry responsible for registering core runtime services.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() is not null)
            {
                return;
            }

            GameObject go = new(nameof(GameBootstrap));
            DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            RegisterCoreServices();
        }

        private static void RegisterCoreServices()
        {
            ServiceLocator.Reset();
            ServiceLocator.Register(new EventBus());
            ServiceLocator.Register(new CommandDispatcher());
        }
    }
}
