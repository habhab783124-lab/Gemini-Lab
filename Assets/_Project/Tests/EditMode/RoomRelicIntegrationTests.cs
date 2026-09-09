#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using GeminiLab.Modules.RoomRelic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeminiLab.Tests.EditMode
{
    public sealed class RoomRelicIntegrationTests
    {
        private readonly List<GameObject> _objects = new();
        private RoomRelicCatalogSO _catalog = null!;
        private RoomRelicService _service = null!;
        private PetSocialService _social = null!;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();
            var clock = new FakeGameClock();
            clock.SetLocal(new DateTime(2026, 9, 9));
            var roster = new PetRoster();
            _social = new PetSocialService(roster, utcNow: () => clock.UtcNow);
            _catalog = ScriptableObject.CreateInstance<RoomRelicCatalogSO>();
            _catalog.gifts = new[] {
                new RoomGiftData { id="desk-gift", giverCharacter="Demon", receiverCharacter="Angel", displaySlotId="desk" },
                new RoomGiftData { id="shelf-gift", giverCharacter="Demon", receiverCharacter="Angel", displaySlotId="shelf" }
            };
            _catalog.notes = new[] { new RoomNoteData { id="note", senderCharacter="Demon", receiverCharacter="Angel", content="hello" } };
            ServiceLocator.Register<IGameClock>(clock);
            ServiceLocator.Register<IPetSocialService>(_social);
            ServiceLocator.Register<IPersistentServiceRegistry>(new PersistentServiceRegistry());
            _service = new RoomRelicService(clock, _social, _catalog, new SuccessRandom());
            ServiceLocator.Register<IRoomRelicService>(_service);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
            _service.Dispose();
            UnityEngine.Object.DestroyImmediate(_catalog);
            ServiceLocator.Reset();
        }

        [Test]
        public void GiftsStayInAuthoredSlotsRegardlessOfAcquisitionOrder()
        {
            RoomRelicView desk = CreateSlot("desk");
            RoomRelicView shelf = CreateSlot("shelf");
            var room = NewObject("room").AddComponent<RoomRelicRoomView>();
            var so = new SerializedObject(room);
            var slots = so.FindProperty("_giftSlots");
            slots.arraySize = 2;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = desk;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = shelf;
            so.ApplyModifiedPropertiesWithoutUndo();
            InvokeLifecycle(room, "Start");
            _service.RestoreJson("{\"version\":2,\"angelRoom\":{},\"devilRoom\":{},\"obtainedGiftIds\":[\"shelf-gift\"]}");
            Assert.IsFalse(desk.HasAnyActiveTarget);
            Assert.IsTrue(shelf.transform.Find("shelf-gift").gameObject.activeSelf);
            _service.RestoreJson("{\"version\":2,\"angelRoom\":{},\"devilRoom\":{},\"obtainedGiftIds\":[\"shelf-gift\",\"desk-gift\"]}");
            Assert.IsTrue(desk.transform.Find("desk-gift").gameObject.activeSelf);
            Assert.IsTrue(shelf.transform.Find("shelf-gift").gameObject.activeSelf);
            InvokeLifecycle(room, "OnDisable");
        }

        [Test]
        public void FailedPopupDoesNotConsumeNote_SuccessfulPopupDoes()
        {
            var router = new UIRouter();
            ServiceLocator.Register<IUIRouter>(router);
            _service.ProcessRoomEntry(RoomId.AngelRoom);
            var interaction = NewObject("note-click").AddComponent<RoomRelicInteraction>();
            LogAssert.Expect(LogType.Warning, "[UIRouter] 未注册的面板：RoomNote");
            interaction.Open();
            Assert.IsNotNull(_service.GetCurrentNote(RoomId.AngelRoom));
            var panel = new NotePanel();
            router.Register(panel);
            interaction.Open();
            Assert.AreEqual("hello", panel.ReadContent);
            Assert.IsNull(_service.GetCurrentNote(RoomId.AngelRoom));
        }

        [Test]
        public void RelicPopupKeepsSpacePageOpen_AndClosingReturnsToIt()
        {
            var router = new UIRouter();
            var space = new TrackingPanel(PanelId.SpaceSys);
            var note = new TrackingPanel(PanelId.RoomNote);
            var detail = new TrackingPanel(PanelId.RoomRelicDetail);
            router.Register(space);
            router.Register(note);
            router.Register(detail);
            router.Open(PanelId.SpaceSys);
            router.Open(PanelId.RoomNote);
            Assert.IsTrue(space.IsOpen);
            router.Open(PanelId.RoomRelicDetail);
            Assert.IsFalse(note.IsOpen);
            Assert.IsTrue(space.IsOpen);
            router.CloseTop();
            Assert.AreEqual(PanelId.SpaceSys, router.Top);
            Assert.IsTrue(space.IsOpen);
        }

        private sealed class TrackingPanel : IUIPanel
        {
            public TrackingPanel(PanelId id) { Id = id; }
            public PanelId Id { get; }
            public bool IsOpen;
            public void OnOpen(object? payload) => IsOpen = true;
            public void OnClose() => IsOpen = false;
        }

        [Test]
        public void BootstrapRecreated_RetainsProgressAndResumesSocialListener()
        {
            var first = NewObject("bootstrap-1").AddComponent<RoomRelicRuntimeBootstrap>();
            BindCatalog(first);
            InvokeLifecycle(first, "Start");
            InvokeLifecycle(first, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            var second = NewObject("bootstrap-2").AddComponent<RoomRelicRuntimeBootstrap>();
            BindCatalog(second);
            InvokeLifecycle(second, "Start");
            Assert.AreSame(_service, ServiceLocator.Resolve<IRoomRelicService>());
            _service.ProcessRoomEntry(RoomId.AngelRoom);
            _social.ApplySpecialEventFriendship(50f);
            Assert.AreEqual(1, _service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        private void BindCatalog(RoomRelicRuntimeBootstrap bootstrap)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_catalog").objectReferenceValue = _catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void EmptyOverlappingSlotDoesNotBlockVisibleRelic()
        {
            var visible = CreateSlot("visible"); var empty = CreateSlot("empty");
            visible.transform.position = empty.transform.position = new Vector3(100,100);
            var visibleHit = visible.gameObject.AddComponent<BoxCollider2D>();
            var emptyHit = empty.gameObject.AddComponent<BoxCollider2D>();
            visible.gameObject.AddComponent<SpriteRenderer>().sortingOrder=200;
            empty.gameObject.AddComponent<SpriteRenderer>().sortingOrder=300;
            visible.Apply("desk-gift"); empty.Apply(null); Physics2D.SyncTransforms();
            Assert.IsTrue(visibleHit.enabled); Assert.IsFalse(emptyHit.enabled);
            Assert.IsTrue(ClickOcclusionUtility.IsTopmostColliderAtWorldPoint(new Vector2(100,100),visibleHit));
            empty.Apply("shelf-gift"); Assert.IsTrue(emptyHit.enabled);
            empty.Apply(null); Assert.IsFalse(emptyHit.enabled);
        }

        [Test]
        public void PlacedGiftCanBeInspectedWithoutConsumingOrAwardingAgain()
        {
            _service.RestoreJson("{\"version\":2,\"angelRoom\":{},\"devilRoom\":{},\"obtainedGiftIds\":[\"desk-gift\"]}");
            var view=CreateSlot("desk"); view.Apply("desk-gift");
            var interaction=view.gameObject.AddComponent<RoomRelicInteraction>();
            var so=new SerializedObject(interaction); so.FindProperty("_kind").intValue=(int)RoomRelicKind.PermanentGift; so.ApplyModifiedPropertiesWithoutUndo();
            var router=new UIRouter(); var panel=new GiftPanel(); router.Register(panel); ServiceLocator.Register<IUIRouter>(router);
            int count=_service.GetPlacedGifts(RoomId.AngelRoom).Count;
            interaction.Open();
            Assert.AreEqual("desk-gift",panel.Gift?.id); Assert.AreEqual(count,_service.GetPlacedGifts(RoomId.AngelRoom).Count);
        }

        private sealed class GiftPanel : IUIPanel
        {
            public PanelId Id => PanelId.RoomGiftObtained;
            public RoomGiftData? Gift;
            public void OnOpen(object? payload) => Gift=payload as RoomGiftData;
            public void OnClose() { }
        }

        // EditMode 不使用 SendMessage 触发运行时回调；直接执行组件入口并断言外部行为。
        private static void InvokeLifecycle(MonoBehaviour component, string method)
        {
            component.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(component, null);
        }

        private RoomRelicView CreateSlot(string id)
        {
            var view = NewObject(id).AddComponent<RoomRelicView>();
            var so = new SerializedObject(view);
            so.FindProperty("_displaySlotId").stringValue = id;
            var variants = so.FindProperty("_variants");
            variants.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                string giftId = i == 0 ? "desk-gift" : "shelf-gift";
                var target = new GameObject(giftId);
                target.transform.SetParent(view.transform);
                variants.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue = giftId;
                variants.GetArrayElementAtIndex(i).FindPropertyRelative("_target").objectReferenceValue = target;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private GameObject NewObject(string name)
        { var go = new GameObject(name); _objects.Add(go); return go; }

        private sealed class SuccessRandom : System.Random { public override double NextDouble() => 0; }
        private sealed class NotePanel : IUIPanel
        {
            public PanelId Id => PanelId.RoomNote;
            public string? ReadContent;
            public void OnOpen(object? payload) => ReadContent = ServiceLocator.Resolve<IRoomRelicService>().GetCurrentNote((RoomId)payload!)?.content;
            public void OnClose() { }
        }
    }
}
