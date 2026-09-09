#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the outdoor pet trigger controller without creating placeholder
    /// interaction objects. Recognized scene objects are serialized explicitly;
    /// unrecognized sign objects remain nullable and can be assigned in Inspector.
    /// </summary>
    public static class WorldMapPetAnimationTriggerAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string SceneRootName = "_SceneRoot";
        private const string LegacyRootName = "WorldMapAnimationTriggers";

        private static readonly string[] AppleTreeNames =
        {
            "大树 2", "大树 3", "大树 4", "大树 5", "苹果树", "苹果树 2", "苹果树 3", "苹果树 4", "苹果树 5"
        };

        private static readonly string[] AngelSignNames =
        {
            "天使标牌", "天使区域标牌", "天使区域的标牌", "标牌_天使", "AngelSign", "AngelSignboard"
        };

        private static readonly string[] DevilSignNames =
        {
            "恶魔标牌", "恶魔区域标牌", "恶魔区域的标牌", "标牌_恶魔", "DevilSign", "DevilSignboard"
        };

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Outdoor Pet Animation Triggers")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapPetAnimation] Stop PlayMode before authoring the outdoor trigger bindings.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveLegacyTriggerObjects();

            GameObject? sceneRoot = GameObject.Find(SceneRootName);
            if (sceneRoot == null)
            {
                Debug.LogError($"[WorldMapPetAnimation] Missing scene root '{SceneRootName}'.");
                return;
            }

            PetController? angelPet = FindPet("Pet_Angel");
            PetController? devilPet = FindPet("Pet_Devil");
            var controller = sceneRoot.GetComponent<WorldMapPetAnimationTriggerController>();
            if (controller == null) controller = sceneRoot.AddComponent<WorldMapPetAnimationTriggerController>();

            controller.ConfigureForAuthoring(angelPet, devilPet);

            Transform[] appleTrees = FindTransforms(AppleTreeNames);
            Transform? wishTree = FindTransform("许愿树", "WishingTree");
            Transform? angelSign = FindTransform(AngelSignNames);
            Transform? devilSign = FindTransform(DevilSignNames);
            controller.ConfigureSceneTargets(appleTrees, wishTree, angelSign, devilSign);

            EditorUtility.SetDirty(sceneRoot);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[WorldMapPetAnimation] Authored {appleTrees.Length} apple-tree target(s), wishing tree={(wishTree != null)}, angel sign={(angelSign != null)}, devil sign={(devilSign != null)}. Existing Pet Animation Clips remain unchanged.");
            if (angelSign == null || devilSign == null)
            {
                Debug.Log("[WorldMapPetAnimation] Assign any missing sign target directly on WorldMapPetAnimationTriggerController in Inspector; no placeholder was created.");
            }
        }

        private static void RemoveLegacyTriggerObjects()
        {
            GameObject? legacyRoot = GameObject.Find(LegacyRootName);
            if (legacyRoot == null) return;

            Object.DestroyImmediate(legacyRoot);
            Debug.Log("[WorldMapPetAnimation] Removed the old debug trigger root; no replacement objects were created.");
        }

        private static PetController? FindPet(string name)
        {
            GameObject? petObject = GameObject.Find(name);
            return petObject != null
                ? petObject.GetComponent<PetController>() ?? petObject.GetComponentInChildren<PetController>(true)
                : null;
        }

        private static Transform[] FindTransforms(string[] names)
        {
            var found = new List<Transform>();
            for (int i = 0; i < names.Length; i++)
            {
                Transform? target = FindTransform(names[i]);
                if (target != null && !found.Contains(target)) found.Add(target);
            }

            return found.ToArray();
        }

        private static Transform? FindTransform(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                GameObject? target = GameObject.Find(names[i]);
                if (target != null) return target.transform;
            }

            return null;
        }
    }
}
#endif
