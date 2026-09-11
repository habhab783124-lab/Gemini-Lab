#if UNITY_EDITOR
using System;
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class PetVisualBindingAuthoring
    {
        [MenuItem("Tools/Gemini-Lab/Pet/Author Visual Bindings")]
        public static void AuthorActiveScene()
        {
            if (Application.isPlaying) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var pet in root.GetComponentsInChildren<PetController>(true)) Author(pet);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // Explicit migration entry point; never runs automatically on import.
        public static void AuthorProject()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            foreach (var open in setup)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(open.path).isDirty)
                    throw new InvalidOperationException("Save open scenes before authoring project bindings.");
            try
            {
                foreach (var name in new[] { "Angel", "Devil" })
                {
                    string path = $"Assets/_Project/Prefabs/Pet/Pet_{name}.prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        foreach (var pet in root.GetComponentsInChildren<PetController>(true)) Author(pet);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                foreach (var area in new[] { "Apartment", "WorldMap" })
                {
                    var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{area}/{area}_Main.unity");
                    AuthorActiveScene();
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        public static void Author(PetController pet)
        {
            var data = new SerializedObject(pet);
            var animator = pet.GetComponent<Animator>();
            var renderer = pet.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                throw new InvalidOperationException($"{pet.name}: author the pet Sprite first.");
            if (animator == null) animator = Undo.AddComponent<Animator>(pet.gameObject);
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = (RuntimeAnimatorController)data.FindProperty("_movementController").objectReferenceValue;
            if (animator.runtimeAnimatorController == null)
                throw new InvalidOperationException($"{pet.name}: missing movement controller.");

            var binding = data.FindProperty("_sleepInteractionVisualObject");
            var visual = (GameObject)binding.objectReferenceValue;
            if (visual == null)
            {
                visual = new GameObject("InteractionVisual");
                Undo.RegisterCreatedObjectUndo(visual, "Author pet interaction visual");
                visual.layer = pet.gameObject.layer;
                visual.transform.SetParent(pet.transform, false);
                var sprite = visual.AddComponent<SpriteRenderer>();
                sprite.sprite = renderer.sprite;
                sprite.sharedMaterial = renderer.sharedMaterial;
                sprite.color = renderer.color;
                sprite.sortingLayerID = renderer.sortingLayerID;
                sprite.sortingOrder = renderer.sortingOrder;
                sprite.enabled = false;
                var detached = visual.AddComponent<Animator>();
                detached.runtimeAnimatorController = (RuntimeAnimatorController)data.FindProperty("_movementController").objectReferenceValue;
                if (detached.runtimeAnimatorController == null) detached.runtimeAnimatorController = animator.runtimeAnimatorController;
                detached.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                detached.enabled = false;
            }
            if (visual.GetComponent<SpriteRenderer>() == null || visual.GetComponent<Animator>() == null)
                throw new InvalidOperationException($"{pet.name}: incomplete existing interaction visual; repair it in Inspector.");
            binding.objectReferenceValue = visual;
            data.FindProperty("_sleepInteractionVisualTransform").objectReferenceValue = visual.transform;
            data.FindProperty("_sleepInteractionVisualAnimator").objectReferenceValue = visual.GetComponent<Animator>();
            data.FindProperty("_sleepInteractionVisualSpriteRenderer").objectReferenceValue = visual.GetComponent<SpriteRenderer>();
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(animator);
        }
    }
}
#endif
