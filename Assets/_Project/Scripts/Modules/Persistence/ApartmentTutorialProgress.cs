#nullable enable
using System;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>Save-slot owned tutorial progress. Missing old-save data means a fresh tutorial.</summary>
    public sealed class ApartmentTutorialProgress : IPersistentService
    {
        [Serializable] private sealed class Data
        {
            public int version = 1;
            public bool dismissed;
            public bool completed;
        }
        private Data _data = new();
        private bool _restored;
        private bool _saving;
        private bool _saveAgain;
        public string Key => "apartment_tutorial";
        public bool ShouldShow => !_data.dismissed;
        public bool Completed => _data.completed;
        public event Action? Changed;

        public static ApartmentTutorialProgress EnsureRegistered()
        {
            if (ServiceLocator.TryResolve(out ApartmentTutorialProgress? existing) && existing != null) return existing;
            var progress = new ApartmentTutorialProgress();
            ServiceLocator.Register(progress);
            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry != null) registry.Register(progress);
            // A late registration may synchronously restore the already-loaded slot.
            progress._restored = false;
            if (ServiceLocator.TryResolve(out EventBus? bus) && bus != null)
                bus.Subscribe<SaveSlotLoadedEvent>(_ => progress.OnSlotLoaded());
            return progress;
        }

        public void Dismiss(bool completed)
        {
            _data.dismissed = true;
            _data.completed |= completed;
            Changed?.Invoke();
        }
        public string CaptureJson() => JsonUtility.ToJson(_data);
        public bool RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var restored = JsonUtility.FromJson<Data>(json);
                if (restored == null || restored.version != 1) return false;
                restored.dismissed |= restored.completed;
                _data = restored;
                _restored = true;
                Changed?.Invoke();
                return true;
            }
            catch (ArgumentException) { return false; }
        }
        private void OnSlotLoaded()
        {
            if (!_restored) _data = new Data();
            _restored = false;
            Changed?.Invoke();
        }
        public async Task SaveAsync()
        {
            _saveAgain = true;
            if (_saving) return;
            _saving = true;
            try
            {
                while (_saveAgain)
                {
                    _saveAgain = false;
                    if (ServiceLocator.TryResolve(out ISaveCoordinator? save) && save != null)
                        await save.SaveAsync("autosave");
                }
            }
            catch (Exception ex) { Debug.LogError($"[ApartmentTutorial] 保存失败：{ex.Message}"); }
            finally { _saving = false; }
        }
    }
}
