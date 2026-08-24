#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Apple;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 将 WorldMap 中作为苹果树的 4 棵大树绑定到苹果领取服务。
    /// 只补脚本和树 ID，不创建或替换树的任何视觉资源。
    /// </summary>
    public static class WorldMapAppleTreeAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private static readonly string[] TreeNames = { "大树 2", "大树 3", "大树 4", "大树 5" };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;
            var nonAppleTree = GameObject.Find("大树 1");
            if (nonAppleTree != null)
            {
                changed |= RemoveAppleTreeBinding(nonAppleTree);
            }

            for (int i = 0; i < TreeNames.Length; i++)
            {
                var tree = GameObject.Find(TreeNames[i]);
                if (tree == null)
                {
                    Debug.LogWarning($"[WorldMapAppleTreeAuthoring] 未找到「{TreeNames[i]}」，跳过");
                    continue;
                }

                var interactable = tree.GetComponent<AppleTreeInteractable>();
                if (interactable == null)
                {
                    interactable = Undo.AddComponent<AppleTreeInteractable>(tree);
                    changed = true;
                }

                var so = new SerializedObject(interactable);
                var treeId = so.FindProperty("_treeId");
                string expectedId = $"world_tree_{i + 2}";
                if (treeId != null && treeId.stringValue != expectedId)
                {
                    treeId.stringValue = expectedId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                AppleTreeFeedback feedback = EnsureFeedback(tree, i + 1, out bool feedbackChanged);
                if (feedbackChanged)
                {
                    changed = true;
                }

                var interactableSo = new SerializedObject(interactable);
                var feedbackProperty = interactableSo.FindProperty("_feedback");
                if (feedbackProperty != null && feedbackProperty.objectReferenceValue != feedback)
                {
                    feedbackProperty.objectReferenceValue = feedback;
                    interactableSo.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[WorldMapAppleTreeAuthoring] 大树 2～5 的苹果领取入口已作者化，大树 1 已排除");
        }

        private static bool RemoveAppleTreeBinding(GameObject tree)
        {
            bool changed = false;
            var interactable = tree.GetComponent<AppleTreeInteractable>();
            if (interactable != null)
            {
                Undo.DestroyObjectImmediate(interactable);
                changed = true;
            }

            var feedbackRoot = tree.transform.Find("AppleTreeFeedback");
            if (feedbackRoot != null)
            {
                Undo.DestroyObjectImmediate(feedbackRoot.gameObject);
                changed = true;
            }

            return changed;
        }

        private static AppleTreeFeedback EnsureFeedback(GameObject tree, int index, out bool changed)
        {
            changed = false;
            Transform? root = tree.transform.Find("AppleTreeFeedback");
            if (root == null)
            {
                var rootObject = new GameObject("AppleTreeFeedback");
                Undo.RegisterCreatedObjectUndo(rootObject, "Create apple tree feedback");
                rootObject.transform.SetParent(tree.transform, false);
                rootObject.transform.localPosition = new Vector3(0f, 4.4f, -0.2f);
                root = rootObject.transform;
                changed = true;
            }

            TMP_Text status = EnsureText(root, "StatusText", "还没成熟哦", new Vector3(0f, 0.35f, 0f), 0.42f, 30, Color.white, out bool statusChanged);
            TMP_Text leftLeaf = EnsureText(root, "LeafLeft", "❧", new Vector3(-0.2f, -0.2f, 0f), 0.5f, 24, new Color(0.35f, 0.8f, 0.3f), out bool leftChanged);
            TMP_Text rightLeaf = EnsureText(root, "LeafRight", "❧", new Vector3(0.2f, -0.2f, 0f), 0.5f, 24, new Color(0.45f, 0.9f, 0.25f), out bool rightChanged);
            changed |= statusChanged || leftChanged || rightChanged;

            var feedback = root.GetComponent<AppleTreeFeedback>();
            if (feedback == null)
            {
                feedback = Undo.AddComponent<AppleTreeFeedback>(root.gameObject);
                changed = true;
            }

            var so = new SerializedObject(feedback);
            SetObjectReference(so, "_statusText", status);
            SetObjectReference(so, "_leftLeaf", leftLeaf);
            SetObjectReference(so, "_rightLeaf", rightLeaf);
            so.ApplyModifiedPropertiesWithoutUndo();
            status.gameObject.SetActive(false);
            leftLeaf.gameObject.SetActive(false);
            rightLeaf.gameObject.SetActive(false);
            return feedback;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            string initialText,
            Vector3 localPosition,
            float fontSize,
            int sortingOrder,
            Color color,
            out bool changed)
        {
            changed = false;
            Transform? existing = parent.Find(name);
            TextMeshPro? text = existing != null ? existing.GetComponent<TextMeshPro>() : null;
            if (text == null)
            {
                var textObject = new GameObject(name, typeof(TextMeshPro));
                Undo.RegisterCreatedObjectUndo(textObject, "Create apple tree feedback text");
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<TextMeshPro>();
                changed = true;
            }

            text.text = initialText;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.transform.localPosition = localPosition;
            text.transform.localScale = Vector3.one;
            var renderer = text.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = sortingOrder;
            }

            return text;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty? property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
#endif
