#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Core
{
    /// <summary>
    /// Lightweight runtime service container for singleton-like services.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        /// <summary>
        /// Registers a service instance by its contract type.
        /// </summary>
        public static void Register<TService>(TService instance) where TService : class
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Services[typeof(TService)] = instance;
        }

        /// <summary>
        /// Attempts to resolve a registered service.
        /// </summary>
        public static bool TryResolve<TService>(out TService? service) where TService : class
        {
            if (Services.TryGetValue(typeof(TService), out object? value))
            {
                service = value as TService;
                return service is not null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Resolves a registered service or throws when missing.
        /// </summary>
        public static TService Resolve<TService>() where TService : class
        {
            if (TryResolve<TService>(out TService? service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service not registered: {typeof(TService).FullName}");
        }

        /// <summary>
        /// Clears all service registrations.
        /// </summary>
        public static void Reset()
        {
            Services.Clear();
        }
    }
}
