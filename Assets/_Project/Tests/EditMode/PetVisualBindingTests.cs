using System.Linq;
using System.Reflection;
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetVisualBindingTests
    {
        [TestCase("Angel")]
        [TestCase("Devil")]
        public void Prefab_HasAuthoredVisualResources(string name)
        {
            var root = PrefabUtility.LoadPrefabContents($"Assets/_Project/Prefabs/Pet/Pet_{name}.prefab");
            try { AssertBinding(root.GetComponent<PetController>()); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [TestCase("Apartment")]
        [TestCase("WorldMap")]
        public void Scene_PetsHaveAuthoredVisualResources(string area)
        {
            string path = $"Assets/_Project/Scenes/{area}/{area}_Main.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
            var previousActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var pets = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PetController>(true)).ToArray();
                Assert.That(pets.Length, Is.EqualTo(2));
                foreach (var pet in pets) AssertBinding(pet);
            }
            finally
            {
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previousActive.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);
            }
        }

        [Test]
        public void BindingCheck_PreservesSceneAnimatorController()
        {
            var root = new GameObject("binding test");
            root.SetActive(false);
            var authored = new AnimatorController();
            var legacy = new AnimatorController();
            try
            {
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = authored;
                var pet = root.AddComponent<PetController>();
                Field("_animator").SetValue(pet, animator);
                Field("_movementController").SetValue(pet, legacy);
                Invoke(pet, "EnsureAnimatorBinding");
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(authored));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(authored);
                Object.DestroyImmediate(legacy);
            }
        }

        [Test]
        public void DetachedVisual_ReusesBindingAndStopsAnimationOnRestore()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Pet/Pet_Angel.prefab");
            try
            {
                var pet = root.GetComponent<PetController>();
                var visual = (GameObject)Field("_sleepInteractionVisualObject").GetValue(pet);
                var renderer = visual.GetComponent<SpriteRenderer>();
                var animator = visual.GetComponent<Animator>();
                var sprite = renderer.sprite;
                var controller = animator.runtimeAnimatorController;
                int count = root.GetComponentsInChildren<Transform>(true).Length;
                Invoke(pet, "EnsureSleepInteractionVisual");
                Invoke(pet, "EnsureSleepInteractionVisual");
                Assert.That(Field("_sleepInteractionVisualObject").GetValue(pet), Is.SameAs(visual));
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
                renderer.enabled = true;
                animator.enabled = true;
                Invoke(pet, "RestoreSleepInteractionVisual");
                Assert.That(renderer.enabled, Is.False);
                Assert.That(animator.enabled, Is.False);
                Assert.That(renderer.sprite, Is.SameAs(sprite));
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void AuthoredChild_KeepsFurniturePoseWhenHiddenBodyMovesOrScales()
        {
            var root = new GameObject("body pose test");
            root.SetActive(false);
            try
            {
                var pet = root.AddComponent<PetController>();
                var visual = new GameObject("authored visual").transform;
                visual.SetParent(root.transform, false);
                Field("_sleepInteractionVisualTransform").SetValue(pet, visual);
                Field("_interactionVisualWorldPosition").SetValue(pet, new Vector3(3, 4, 0));
                Field("_interactionVisualWorldScale").SetValue(pet, new Vector3(.5f, .5f, 1));
                Field("_interactionVisualWorldRotation").SetValue(pet, Quaternion.identity);
                foreach (float scale in new[] { .5f, 2f })
                {
                    root.transform.position = new Vector3(8 * scale, 9, 0);
                    root.transform.localScale = new Vector3(scale, scale, 1);
                    Invoke(pet, "ApplyAuthoredInteractionVisualPose");
                    Assert.That(Vector3.Distance(visual.position, new Vector3(3, 4, 0)), Is.LessThan(.0001f));
                    Assert.That(Vector3.Distance(visual.lossyScale, new Vector3(.5f, .5f, 1)), Is.LessThan(.0001f));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AssertBinding(PetController pet)
        {
            Assert.That(pet, Is.Not.Null);
            var data = new SerializedObject(pet);
            var visual = (GameObject)data.FindProperty("_sleepInteractionVisualObject").objectReferenceValue;
            Assert.That(visual, Is.Not.Null, pet.name);
            Assert.That(visual.transform.parent, Is.EqualTo(pet.transform));
            var renderer = visual.GetComponent<SpriteRenderer>();
            var animator = visual.GetComponent<Animator>();
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(renderer.enabled, Is.False);
            Assert.That(animator.enabled, Is.False);
            Assert.That(pet.GetComponent<Animator>().runtimeAnimatorController, Is.Not.Null);
            Assert.That(data.FindProperty("_sleepInteractionVisualTransform").objectReferenceValue, Is.EqualTo(visual.transform));
            Assert.That(data.FindProperty("_sleepInteractionVisualAnimator").objectReferenceValue, Is.EqualTo(animator));
            Assert.That(data.FindProperty("_sleepInteractionVisualSpriteRenderer").objectReferenceValue, Is.EqualTo(renderer));
        }

        private static FieldInfo Field(string name) => typeof(PetController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static void Invoke(PetController pet, string name) => typeof(PetController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pet, null);
    }
}
