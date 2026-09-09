#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Core.Persistence
{
    /// <summary>
    /// 内存字典实现；线程安全保持在"单主线程 Unity"假设下——多线程访问属误用。
    /// </summary>
    public sealed class PersistentServiceRegistry : IPersistentServiceRegistry
    {
        private readonly Dictionary<string, IPersistentService> _byKey = new();
        public event Action<IPersistentService>? Registered;

        public void Register(IPersistentService service)
        {
            if (service is null || string.IsNullOrEmpty(service.Key))
            {
                return;
            }

            _byKey[service.Key] = service;
            Registered?.Invoke(service);
        }

        public void Unregister(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _byKey.Remove(key);
        }

        public IPersistentService? TryGet(string key)
        {
            return _byKey.TryGetValue(key, out var s) ? s : null;
        }

        public IReadOnlyCollection<IPersistentService> All => _byKey.Values;
    }
}
