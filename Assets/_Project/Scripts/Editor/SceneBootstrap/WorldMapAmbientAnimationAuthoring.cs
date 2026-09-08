#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Binds the existing WorldMap visual nodes to the ambient animation
    /// controller.  This pass only adds a component and serialized references;
    /// it never creates runtime visual objects.
    /// </summary>
    public static class WorldMapAmbientAnimationAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Ambient Animations")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapAmbientAnimation] Skip authoring while in PlayMode.");
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[WorldMapAmbientAnimation] Missing scene: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject? host = FindInScene(scene, "_SceneRoot");
            if (host == null)
            {
                Debug.LogError("[WorldMapAmbientAnimation] Missing _SceneRoot host.");
                return;
            }

            WorldMapAmbientAnimationController controller = host.GetComponent<WorldMapAmbientAnimationController>()
                ?? Undo.AddComponent<WorldMapAmbientAnimationController>(host);
            if (controller == null)
            {
                Debug.LogError("[WorldMapAmbientAnimation] Unable to add controller.");
                return;
            }

            SpriteRenderer? cloud = FindRenderer(scene, "WorldMapWeatherClouds");
            Transform? flowerRoot = FindInScene(scene, "WorldMapPlacedFlowers")?.transform;
            List<WorldMapAmbientAnimationController.TreeBinding> trees = FindTreeBindings(scene);
            controller.ConfigureForAuthoring(cloud, flowerRoot, trees);

            // Keep the motion visibly gentle in the authored scene. Runtime
            // code only animates these existing transforms.
            SerializedObject serialized = new(controller);
            serialized.FindProperty("_singleFlowerRotationAngle")!.floatValue = 3.2f;
            serialized.FindProperty("_singleFlowerRotationSpeed")!.floatValue = 0.9f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldMapAmbientAnimation] Bound cloud={cloud != null}, single-flower root={flowerRoot != null}, trees={trees.Count}, single visuals={controller.SingleFlowerVisualCount}.");
        }

        private static List<WorldMapAmbientAnimationController.TreeBinding> FindTreeBindings(UnityEngine.SceneManagement.Scene scene)
        {
            var treeNames = new HashSet<string>
            {
                "\u8BB8\u613F\u6811",
                "\u5927\u6811 2",
                "\u5927\u6811 3",
                "\u5927\u6811 4",
                "\u5927\u6811 5"
            };
            var spriteNames = new HashSet<string>
            {
                "\u6811 1",
                "\u6811 2",
                "\u6811 3",
                "\u6811 4",
                "\u6811 5"
            };

            var result = new List<WorldMapAmbientAnimationController.TreeBinding>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                {
                    SpriteRenderer renderer = renderers[index];
                    if (renderer.sprite == null) continue;
                    if (!treeNames.Contains(renderer.gameObject.name) && !spriteNames.Contains(renderer.sprite.name)) continue;
                    if (renderer.bounds.size.y < 4f) continue;

                    Vector2 localPivot = new(0f, renderer.sprite.bounds.min.y);
                    result.Add(new WorldMapAmbientAnimationController.TreeBinding(renderer.transform, localPivot));
                }
            }

            return result
                .GroupBy(binding => binding.Target?.GetInstanceID() ?? 0)
                .Select(group => group.First())
                .ToList();
        }

        private static SpriteRenderer? FindRenderer(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            return FindInScene(scene, objectName)?.GetComponent<SpriteRenderer>();
        }

        private static GameObject? FindInScene(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject? match = FindChildByName(root.transform, objectName);
                if (match != null) return match;
            }

            return null;
        }

        private static GameObject? FindChildByName(Transform root, string objectName)
        {
            if (root.name == objectName) return root.gameObject;
            for (int index = 0; index < root.childCount; index++)
            {
                GameObject? match = FindChildByName(root.GetChild(index), objectName);
                if (match != null) return match;
            }

            return null;
        }
    }
}
#endif
