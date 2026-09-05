#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.Apple;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the apple-tree click/drop presentation into WorldMap_Main.
    /// Every drop slot is a saved Scene object; the runtime never creates one.
    /// </summary>
    public static class WorldMapAppleTreeAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string AppleSpritePath = "Assets/_Project/Art/WorldMap/苹果云背景补充/apple.png";
        private static readonly string[] TreeNames = { "大树 2", "大树 3", "大树 4", "大树 5" };
        private static readonly float[] SlotXOffsets = { -1.35f, 0f, 1.35f };

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Apple Tree Drops")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapAppleTree] Skip authoring while in PlayMode.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject? sceneRoot = FindInScene(scene, "_SceneRoot");
            if (sceneRoot == null)
            {
                Debug.LogError("[WorldMapAppleTree] Missing _SceneRoot.");
                return;
            }

            Sprite? appleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AppleSpritePath);
            if (appleSprite == null)
            {
                Debug.LogError($"[WorldMapAppleTree] Missing apple Sprite: {AppleSpritePath}");
                return;
            }

            Transform dropRoot = EnsureDropRoot(sceneRoot.transform, out bool rootChanged);
            bool changed = rootChanged;
            int boundCount = 0;

            RemoveAppleBinding(FindInScene(scene, "大树 1"), ref changed);
            RemoveAppleBinding(FindInScene(scene, "许愿树"), ref changed);

            for (int treeIndex = 0; treeIndex < TreeNames.Length; treeIndex++)
            {
                GameObject? tree = FindInScene(scene, TreeNames[treeIndex]);
                if (tree == null)
                {
                    Debug.LogWarning($"[WorldMapAppleTree] 未找到「{TreeNames[treeIndex]}」，跳过。");
                    continue;
                }

                string treeId = $"world_tree_{treeIndex + 2}";
                AppleTreeFeedback? feedback = tree.transform.Find("AppleTreeFeedback")?.GetComponent<AppleTreeFeedback>();
                AppleDropSlot[] slots = new AppleDropSlot[3];
                SpriteRenderer? treeRenderer = tree.GetComponent<SpriteRenderer>();
                float groundY = treeRenderer != null
                    ? treeRenderer.bounds.min.y + 0.35f
                    : tree.transform.position.y - 4f;

                for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    Vector3 worldPosition = new(
                        tree.transform.position.x + SlotXOffsets[slotIndex],
                        groundY,
                        tree.transform.position.z - 0.25f);
                    slots[slotIndex] = EnsureDropSlot(
                        dropRoot,
                        treeId,
                        slotIndex,
                        worldPosition,
                        appleSprite,
                        out bool slotChanged);
                    changed |= slotChanged;
                }

                AppleTreeDropController controller =
                    tree.GetComponent<AppleTreeDropController>() ??
                    Undo.AddComponent<AppleTreeDropController>(tree);
                if (controller == null)
                {
                    Debug.LogError($"[WorldMapAppleTree] 无法为 {tree.name} 添加掉落控制器。");
                    continue;
                }

                SerializedObject controllerSo = new(controller);
                SetString(controllerSo, "_treeId", treeId);
                SetObjectReference(controllerSo, "_shakeTarget", tree.transform);
                SetObjectReference(controllerSo, "_feedback", feedback);
                Vector2 pivot = treeRenderer != null && treeRenderer.sprite != null
                    ? new Vector2(0f, treeRenderer.sprite.bounds.min.y)
                    : Vector2.zero;
                SerializedProperty? pivotProperty = controllerSo.FindProperty("_shakeLocalPivot");
                if (pivotProperty != null) pivotProperty.vector2Value = pivot;
                SerializedProperty? slotsProperty = controllerSo.FindProperty("_dropSlots");
                if (slotsProperty != null)
                {
                    slotsProperty.arraySize = slots.Length;
                    for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                    {
                        slotsProperty.GetArrayElementAtIndex(slotIndex).objectReferenceValue = slots[slotIndex];
                    }
                }
                SetFloat(controllerSo, "_shakeAmplitudeDegrees", 9f);
                SetInt(controllerSo, "_shakeCycles", 6);
                SetFloat(controllerSo, "_shakeDurationSeconds", 0.58f);
                controllerSo.ApplyModifiedPropertiesWithoutUndo();

                AppleTreeInteractable interactable = tree.GetComponent<AppleTreeInteractable>() ??
                    Undo.AddComponent<AppleTreeInteractable>(tree);
                SerializedObject interactableSo = new(interactable);
                SetString(interactableSo, "_treeId", treeId);
                SetObjectReference(interactableSo, "_feedback", feedback);
                SetObjectReference(interactableSo, "_dropController", controller);
                interactableSo.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
                boundCount++;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[WorldMapAppleTree] 已作者化 {boundCount} 棵苹果树及其 3 个掉落槽位；大树 1/许愿树已排除。");
        }

        private static Transform EnsureDropRoot(Transform sceneRoot, out bool changed)
        {
            changed = false;
            Transform? existing = sceneRoot.Find("WorldMapAppleDrops");
            if (existing != null) return existing;

            GameObject root = new("WorldMapAppleDrops");
            Undo.RegisterCreatedObjectUndo(root, "Create WorldMap apple drops");
            root.transform.SetParent(sceneRoot, false);
            root.transform.localPosition = Vector3.zero;
            changed = true;
            return root.transform;
        }

        private static AppleDropSlot EnsureDropSlot(
            Transform parent,
            string treeId,
            int index,
            Vector3 worldPosition,
            Sprite appleSprite,
            out bool changed)
        {
            changed = false;
            string objectName = $"{treeId}_AppleDrop_{index + 1:00}";
            Transform? existing = parent.Find(objectName);
            GameObject slotObject;
            if (existing == null)
            {
                slotObject = new GameObject(objectName, typeof(SpriteRenderer), typeof(BoxCollider2D));
                Undo.RegisterCreatedObjectUndo(slotObject, "Create authored apple drop slot");
                slotObject.transform.SetParent(parent, false);
                changed = true;
            }
            else
            {
                slotObject = existing.gameObject;
            }

            slotObject.transform.position = worldPosition;
            slotObject.transform.localScale = Vector3.one * 0.22f;
            SpriteRenderer renderer = slotObject.GetComponent<SpriteRenderer>() ??
                Undo.AddComponent<SpriteRenderer>(slotObject);
            renderer.sprite = appleSprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 9050;
            renderer.enabled = false;

            BoxCollider2D collider = slotObject.GetComponent<BoxCollider2D>() ??
                Undo.AddComponent<BoxCollider2D>(slotObject);
            collider.isTrigger = false;
            collider.size = appleSprite.bounds.size;
            collider.offset = appleSprite.bounds.center;
            collider.enabled = false;

            Transform? textTransform = slotObject.transform.Find("CollectionText");
            TextMeshPro? text = textTransform != null
                ? textTransform.GetComponent<TextMeshPro>()
                : null;
            if (text == null)
            {
                GameObject textObject = new("CollectionText", typeof(TextMeshPro));
                Undo.RegisterCreatedObjectUndo(textObject, "Create apple collection feedback");
                textObject.transform.SetParent(slotObject.transform, false);
                text = textObject.GetComponent<TextMeshPro>();
                changed = true;
            }

            text.text = string.Empty;
            text.fontSize = 0.52f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color(0.35f, 0.15f, 0.05f, 1f);
            text.transform.localPosition = new Vector3(0f, 1.5f, -0.15f);
            text.transform.localScale = Vector3.one;
            Renderer? textRenderer = text.GetComponent<Renderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerName = "Default";
                textRenderer.sortingOrder = 9060;
            }
            text.gameObject.SetActive(false);

            AppleDropSlot dropSlot = slotObject.GetComponent<AppleDropSlot>() ??
                Undo.AddComponent<AppleDropSlot>(slotObject);
            SerializedObject slotSo = new(dropSlot);
            SetObjectReference(slotSo, "_renderer", renderer);
            SetObjectReference(slotSo, "_collider", collider);
            SetObjectReference(slotSo, "_feedbackText", text);
            SerializedProperty? duration = slotSo.FindProperty("_feedbackDurationSeconds");
            if (duration != null) duration.floatValue = 2f;
            slotSo.ApplyModifiedPropertiesWithoutUndo();
            dropSlot.HideImmediate();
            return dropSlot;
        }

        private static void RemoveAppleBinding(GameObject? tree, ref bool changed)
        {
            if (tree == null) return;
            AppleTreeInteractable? interactable = tree.GetComponent<AppleTreeInteractable>();
            if (interactable != null)
            {
                Undo.DestroyObjectImmediate(interactable);
                changed = true;
            }

            AppleTreeDropController? controller = tree.GetComponent<AppleTreeDropController>();
            if (controller != null)
            {
                Undo.DestroyObjectImmediate(controller);
                changed = true;
            }
        }

        private static void SetString(SerializedObject so, string name, string value)
        {
            SerializedProperty? property = so.FindProperty(name);
            if (property != null) property.stringValue = value;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            SerializedProperty? property = so.FindProperty(name);
            if (property != null) property.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            SerializedProperty? property = so.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject so, string name, Object? value)
        {
            SerializedProperty? property = so.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static GameObject? FindInScene(Scene scene, string objectName)
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
