#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Persistence;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class DeferredSaveRestoreTests
    {
        [Test]
        public void LateRegistration_RestoresLoadedDataExactlyOnce()
        {
            var store = new MemoryStore();
            var registry = new PersistentServiceRegistry();
            using var coordinator = new SaveCoordinator(store, registry, new FakeGameClock(), null);
            var bundle = new SaveBundle();
            bundle.SetService("room_relic", "saved-gifts");
            store.SaveNow("slot_1", bundle);
            Assert.IsTrue(coordinator.LoadAsync("slot_1").GetAwaiter().GetResult());
            var service = new RecordingService();
            registry.Register(service);
            registry.Register(service);
            Assert.AreEqual("saved-gifts", service.Json);
            Assert.AreEqual(1, service.RestoreCount);
        }

        [Test]
        public void SaveBeforeRoomLoads_PreservesDeferredProgress()
        {
            var store = new MemoryStore();
            var registry = new PersistentServiceRegistry();
            using var coordinator = new SaveCoordinator(store, registry, new FakeGameClock(), null);
            var bundle = new SaveBundle();
            bundle.SetService("room_relic", "saved-gifts");
            store.SaveNow("slot_1", bundle);
            coordinator.LoadAsync("slot_1").GetAwaiter().GetResult();
            coordinator.SaveAsync("autosave").GetAwaiter().GetResult();
            Assert.AreEqual("saved-gifts", store.LoadAsync<SaveBundle>("autosave").GetAwaiter().GetResult()!.GetService("room_relic"));
        }

        [Test]
        public void LoadingAnotherSlot_DiscardsPreviousPendingData()
        {
            var store = new MemoryStore();
            var registry = new PersistentServiceRegistry();
            using var coordinator = new SaveCoordinator(store, registry, new FakeGameClock(), null);
            var bundle = new SaveBundle();
            bundle.SetService("room_relic", "old-slot");
            store.SaveNow("slot_1", bundle);
            store.SaveNow("slot_2", new SaveBundle());
            coordinator.LoadAsync("slot_1").GetAwaiter().GetResult();
            coordinator.LoadAsync("slot_2").GetAwaiter().GetResult();
            var service = new RecordingService();
            registry.Register(service);
            Assert.AreEqual(0, service.RestoreCount);
        }

        private sealed class RecordingService : IPersistentService
        {
            public string Key => "room_relic";
            public string Json = "initial";
            public int RestoreCount;
            public string CaptureJson() => Json;
            public bool RestoreJson(string json) { Json = json; RestoreCount++; return true; }
        }

        private sealed class MemoryStore : ISaveSystem
        {
            private readonly Dictionary<string, object> _slots = new();
            public void SaveNow<T>(string slot, T data) => _slots[slot] = data!;
            public Task SaveAsync<T>(string slot, T data, CancellationToken cancellationToken = default)
            { SaveNow(slot, data); return Task.CompletedTask; }
            public Task<T?> LoadAsync<T>(string slot, CancellationToken cancellationToken = default) where T : class
            { return Task.FromResult(_slots.TryGetValue(slot, out object value) ? value as T : null); }
            public Task DeleteSlotAsync(string slot, CancellationToken cancellationToken = default)
            { _slots.Remove(slot); return Task.CompletedTask; }
        }
    }
}
