#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Core.Events
{
    /// <summary>
    /// Type-keyed in-memory event bus.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();
        private readonly object _syncRoot = new();

        /// <summary>
        /// Subscribes to an event payload type.
        /// </summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            Type key = typeof(TEvent);
            lock (_syncRoot)
            {
                if (_handlers.TryGetValue(key, out Delegate? existing))
                {
                    _handlers[key] = Delegate.Combine(existing, callback);
                }
                else
                {
                    _handlers.Add(key, callback);
                }
            }

            return new Subscription(() => Unsubscribe(callback));
        }

        /// <summary>
        /// Unsubscribes from an event payload type.
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> callback)
        {
            if (callback is null)
            {
                return;
            }

            Type key = typeof(TEvent);
            lock (_syncRoot)
            {
                if (!_handlers.TryGetValue(key, out Delegate? existing))
                {
                    return;
                }

                Delegate? updated = Delegate.Remove(existing, callback);
                if (updated is null)
                {
                    _handlers.Remove(key);
                }
                else
                {
                    _handlers[key] = updated;
                }
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers.
        /// </summary>
        public void Publish<TEvent>(TEvent payload)
        {
            Type key = typeof(TEvent);
            Delegate? existing;
            lock (_syncRoot)
            {
                if (!_handlers.TryGetValue(key, out existing))
                {
                    return;
                }
            }

            if (existing is Action<TEvent> callback)
            {
                Delegate[] invocationList = callback.GetInvocationList();
                foreach (Delegate handler in invocationList)
                {
                    try
                    {
                        ((Action<TEvent>)handler).Invoke(payload);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all subscriptions.
        /// </summary>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _handlers.Clear();
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _disposeAction;
            private bool _disposed;

            public Subscription(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _disposeAction.Invoke();
            }
        }
    }
}
