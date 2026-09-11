#nullable enable
using System;
using System.Reflection;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.UI;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ApartmentOnboardingTests
    {
        [TearDown] public void Cleanup() => ServiceLocator.Reset();

        [Test] public void SkipSurvivesSaveWithoutClaimingCompletion()
        {
            var progress = new ApartmentTutorialProgress();
            Assert.That(progress.ShouldShow, Is.True);
            progress.Dismiss(false);
            var restored = new ApartmentTutorialProgress();
            Assert.That(restored.RestoreJson(progress.CaptureJson()), Is.True);
            Assert.That(restored.ShouldShow, Is.False);
            Assert.That(restored.Completed, Is.False);
        }
        [Test] public void ReplayingAndSkippingDoesNotUndoCompletion()
        {
            var progress = new ApartmentTutorialProgress(); progress.Dismiss(true); progress.Dismiss(false);
            var restored = new ApartmentTutorialProgress(); restored.RestoreJson(progress.CaptureJson());
            Assert.That(restored.Completed, Is.True);
        }
        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"version\":99}")]
        public void InvalidSaveDoesNotOverwriteCurrentProgress(string json)
        {
            var progress = new ApartmentTutorialProgress(); progress.Dismiss(true);
            Assert.That(progress.RestoreJson(json), Is.False);
            Assert.That(progress.Completed, Is.True);
        }
        [Test] public void LoadingAnOlderSlotDoesNotInheritAnotherSlotsDismissal()
        {
            ServiceLocator.Reset();
            ServiceLocator.Register<IPersistentServiceRegistry>(new PersistentServiceRegistry());
            var bus = new EventBus(); ServiceLocator.Register(bus);
            var progress = ApartmentTutorialProgress.EnsureRegistered(); progress.Dismiss(true);
            bus.Publish(new SaveSlotLoadedEvent("old_slot_without_tutorial"));
            Assert.That(progress.ShouldShow, Is.True);
            Assert.That(progress.Completed, Is.False);
            progress.RestoreJson("{\"version\":1,\"dismissed\":true,\"completed\":true}");
            bus.Publish(new SaveSlotLoadedEvent("completed_slot"));
            Assert.That(progress.ShouldShow, Is.False);
        }
        [Test] public void ModalLeasesAreIndependentAndIdempotent()
        {
            var first = GameplayInputBlock.Acquire(); var second = GameplayInputBlock.Acquire();
            try
            {
                first.Dispose(); first.Dispose(); Assert.That(GameplayInputBlock.IsBlocked, Is.True);
                second.Dispose(); Assert.That(GameplayInputBlock.IsBlocked, Is.False);
            }
            finally { first.Dispose(); second.Dispose(); }
        }
        [Test] public void HoverChoosesFrontmostFurnitureWithoutConsumingTheClick()
        {
            var root = new GameObject("HoverTest");
            try
            {
                var presenter = root.AddComponent<ApartmentFurnitureSelectionPresenter>();
                var targets = new GameObject[2]; var highlights = new GameObject[2];
                for (int i = 0; i < 2; i++)
                {
                    targets[i] = new GameObject("Target" + i, typeof(SpriteRenderer), typeof(BoxCollider2D));
                    targets[i].transform.SetParent(root.transform);
                    targets[i].transform.position = new Vector3(9000, 9000, 0);
                    targets[i].GetComponent<SpriteRenderer>().sortingOrder = i + 10000;
                    targets[i].GetComponent<BoxCollider2D>().size = Vector2.one;
                    highlights[i] = new GameObject("Highlight" + i); highlights[i].transform.SetParent(root.transform); highlights[i].SetActive(false);
                }
                var so = new SerializedObject(presenter); var entries = so.FindProperty("_entries"); entries.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    entries.GetArrayElementAtIndex(i).FindPropertyRelative("_target").objectReferenceValue = targets[i];
                    entries.GetArrayElementAtIndex(i).FindPropertyRelative("_highlight").objectReferenceValue = highlights[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo(); Physics2D.SyncTransforms();
                Assert.That(presenter.TryHandleWorldPoint(new Vector2(9000,9000)), Is.False);
                Assert.That(highlights[1].activeSelf, Is.True); Assert.That(highlights[0].activeSelf, Is.False);
                targets[1].SetActive(false); Physics2D.SyncTransforms();
                Assert.That(presenter.SetHoverWorldPoint(new Vector2(9000,9000)), Is.True);
                Assert.That(highlights[0].activeSelf, Is.True); Assert.That(highlights[1].activeSelf, Is.False);
                presenter.SetHoverWorldPoint(new Vector2(9500,9500)); Assert.That(presenter.HasSelection, Is.False);
                Assert.That(highlights[0].activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void TutorialClampsPagesAndReleasesInputOnDisableCallback()
        {
            var initialLoad = typeof(AutoSaveManager).GetProperty("InitialLoadCompleted", BindingFlags.Static|BindingFlags.Public)!;
            bool originalLoad = AutoSaveManager.InitialLoadCompleted;
            var root = new GameObject("TutorialTest");
            try
            {
                ServiceLocator.Reset(); initialLoad.SetValue(null, true);
                var view = root.AddComponent<ApartmentTutorialController>();
                var panel = new GameObject("Panel"); panel.transform.SetParent(root.transform); panel.SetActive(false);
                var so = new SerializedObject(view); so.FindProperty("_panel").objectReferenceValue = panel;
                var pages = so.FindProperty("_pages"); pages.arraySize = 6;
                for (int i = 0; i < 6; i++) { var page = new GameObject("Page"+i); page.transform.SetParent(panel.transform); pages.GetArrayElementAtIndex(i).objectReferenceValue = page; }
                so.ApplyModifiedPropertiesWithoutUndo();
                view.Open(); Assert.That(view.IsOpen, Is.True); Assert.That(GameplayInputBlock.IsBlocked, Is.True);
                view.Previous(); Assert.That(view.PageIndex, Is.Zero);
                for (int i = 0; i < 10; i++) view.Next(); Assert.That(view.PageIndex, Is.EqualTo(5));
                view.Finish(); Assert.That(view.IsOpen, Is.False); Assert.That(GameplayInputBlock.IsBlocked, Is.False);
                view.Open(); Assert.That(view.PageIndex, Is.Zero);
                // EditMode does not dispatch the lifecycle of a non-ExecuteAlways component.
                typeof(ApartmentTutorialController).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null); Assert.That(panel.activeSelf, Is.False); Assert.That(GameplayInputBlock.IsBlocked, Is.False);
            }
            finally { typeof(ApartmentTutorialController).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(root.GetComponent<ApartmentTutorialController>(), null); Object.DestroyImmediate(root); initialLoad.SetValue(null, originalLoad); }
        }
        [Test] public void HoverHitsTheVisibleTopBeyondTheWalkingFootprint()
        {
            var root = new GameObject("VisualMeshHoverTest");
            var texture = new Texture2D(32, 128);
            var sprite = Sprite.Create(texture, new Rect(0,0,32,128), new Vector2(0.5f,0.5f), 16, 0, SpriteMeshType.FullRect);
            try
            {
                var target = new GameObject("TallFurniture", typeof(SpriteRenderer), typeof(BoxCollider2D)); target.transform.SetParent(root.transform);
                target.transform.position = new Vector3(9000,9000,0); var renderer=target.GetComponent<SpriteRenderer>(); renderer.sprite=sprite;
                var highlight = new GameObject("Highlight"); highlight.transform.SetParent(root.transform); highlight.SetActive(false);
                var view=root.AddComponent<ApartmentFurnitureSelectionPresenter>(); var so=new SerializedObject(view); var entries=so.FindProperty("_entries"); entries.arraySize=1;
                var entry=entries.GetArrayElementAtIndex(0); entry.FindPropertyRelative("_target").objectReferenceValue=target;
                entry.FindPropertyRelative("_highlight").objectReferenceValue=highlight; entry.FindPropertyRelative("_hitRenderer").objectReferenceValue=renderer;
                so.ApplyModifiedPropertiesWithoutUndo(); Physics2D.SyncTransforms();
                var point=new Vector2(9000,9003);
                Assert.That(target.GetComponent<BoxCollider2D>().OverlapPoint(point), Is.False);
                Assert.That(view.SetHoverWorldPoint(point), Is.True);
                Assert.That(highlight.activeSelf, Is.True);
                renderer.enabled=false; view.SetHoverWorldPoint(point); Assert.That(highlight.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
    }
}
