#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>事件：某槽位已写入完成。</summary>
    public readonly struct SaveSlotCommittedEvent
    {
        public SaveSlotCommittedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>槽位已读取；已注册服务完成恢复，其余数据在服务注册时补恢复。</summary>
    public readonly struct SaveSlotLoadedEvent
    {
        public SaveSlotLoadedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>事件：某槽位已删除。</summary>
    public readonly struct SaveSlotDeletedEvent
    {
        public SaveSlotDeletedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>
    /// <see cref="ISaveCoordinator"/> 默认实现。
    /// Save 流程：
    ///   遍历 Registry → Capture → 塞进 SaveBundle → SaveSystem.SaveAsync
    /// Load 流程：
    ///   SaveSystem.LoadAsync → 遍历 bundle.ServiceKeys → Registry.TryGet → Restore
    /// </summary>
    public sealed class SaveCoordinator : ISaveCoordinator, IDisposable
    {
        private static readonly string[] _defaultSlots = { "slot_1", "slot_2", "slot_3" };

        private readonly ISaveSystem _saveSystem;
        private readonly IPersistentServiceRegistry _registry;
        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;
        private readonly Dictionary<string, string> _pendingRestores = new();

        public SaveCoordinator(
            ISaveSystem saveSystem,
            IPersistentServiceRegistry registry,
            IGameClock clock,
            EventBus? eventBus)
        {
            _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            _registry.Registered += RestorePendingService;
        }

        public IReadOnlyList<string> DefaultSlotIds => _defaultSlots;

        public async Task<IReadOnlyList<SlotSummary>> ListSlotsAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<SlotSummary>(_defaultSlots.Length);
            foreach (var slot in _defaultSlots)
            {
                var bundle = await _saveSystem.LoadAsync<SaveBundle>(slot, cancellationToken).ConfigureAwait(true);
                if (bundle is null)
                {
                    result.Add(new SlotSummary(slot, exists: false, "", "", 0f));
                }
                else
                {
                    result.Add(new SlotSummary(slot, exists: true, bundle.CreatedAtIso, bundle.LastSavedAtIso, bundle.PlayTimeSeconds));
                }
            }
            return result;
        }

        public async Task SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("slotId cannot be empty", nameof(slotId));
            }

            // 先读老 bundle 以保留 CreatedAtIso；没有则新建
            var existing = await _saveSystem.LoadAsync<SaveBundle>(slotId, cancellationToken).ConfigureAwait(true);
            string nowIso = _clock.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var bundle = existing ?? new SaveBundle
            {
                SlotId = slotId,
                CreatedAtIso = nowIso
            };
            bundle.SlotId = slotId;
            bundle.LastSavedAtIso = nowIso;
            // PlayTimeSeconds 这一阶段先保持不变；待接真实 session 计时再累加

            // 重置并按当前 Registry 重新写入
            bundle.ServiceKeys.Clear();
            bundle.ServiceJsons.Clear();
            // 尚未进入其场景的模块也必须随当前进度保存，不能丢掉刚读入的数据。
            foreach (var pending in _pendingRestores)
            {
                bundle.SetService(pending.Key, pending.Value);
            }
            foreach (var svc in _registry.All)
            {
                try
                {
                    if (!_pendingRestores.ContainsKey(svc.Key))
                        bundle.SetService(svc.Key, svc.CaptureJson() ?? string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveCoordinator] Capture failed for '{svc.Key}': {ex.Message}");
                }
            }

            await _saveSystem.SaveAsync(slotId, bundle, cancellationToken).ConfigureAwait(true);
            _eventBus?.Publish(new SaveSlotCommittedEvent(slotId));
        }

        public async Task<bool> LoadAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId)) return false;

            var bundle = await _saveSystem.LoadAsync<SaveBundle>(slotId, cancellationToken).ConfigureAwait(true);
            if (bundle is null) return false;

            // 切换存档槽时，不能把上一槽尚未恢复的数据带入新槽。
            _pendingRestores.Clear();
            for (int i = 0; i < bundle.ServiceKeys.Count; i++)
            {
                string key = bundle.ServiceKeys[i];
                string json = i < bundle.ServiceJsons.Count ? bundle.ServiceJsons[i] : string.Empty;
                var svc = _registry.TryGet(key);
                if (svc is null)
                {
                    _pendingRestores[key] = json;
                    continue;
                }
                try
                {
                    if (!svc.RestoreJson(json))
                    {
                        _pendingRestores[key] = json;
                        Debug.LogWarning($"[SaveCoordinator] Restore '{key}' 返回 false");
                    }
                }
                catch (Exception ex)
                {
                    _pendingRestores[key] = json;
                    Debug.LogWarning($"[SaveCoordinator] Restore '{key}' 抛异常：{ex.Message}");
                }
            }

            _eventBus?.Publish(new SaveSlotLoadedEvent(slotId));
            return true;
        }

        private void RestorePendingService(IPersistentService service)
        {
            if (!_pendingRestores.TryGetValue(service.Key, out string json)) return;
            try
            {
                if (service.RestoreJson(json))
                    _pendingRestores.Remove(service.Key);
                else
                    Debug.LogWarning($"[SaveCoordinator] Deferred restore '{service.Key}' 返回 false，保留原数据");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveCoordinator] Deferred restore '{service.Key}' 失败：{ex.Message}");
            }
        }

        public void Dispose() => _registry.Registered -= RestorePendingService;

        public async Task DeleteAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId)) return;
            await _saveSystem.DeleteSlotAsync(slotId, cancellationToken).ConfigureAwait(true);
            _eventBus?.Publish(new SaveSlotDeletedEvent(slotId));
        }
    }
}
