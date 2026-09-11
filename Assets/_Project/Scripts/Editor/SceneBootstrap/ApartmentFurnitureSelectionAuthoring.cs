#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Incrementally authors indoor furniture selection feedback. It never rebuilds
    /// the apartment scene; it only adds/updates the independent selection nodes.
    /// </summary>
    public static class ApartmentFurnitureSelectionAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string FurnitureRootName = "Furniture";
        private const string PresenterName = "ApartmentFurnitureSelection";
        private const string MessageName = "FurnitureSelectionMessage";
        private const string HighlightName = "FurnitureSelectionHighlight";
        private const string OutlineMaterialPath = "Assets/_Project/Art/WorldMap/UI/garden_week/FurnitureSelectionOutline.mat";
        private const string FontPath = "Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset";

        private static readonly SelectionDefinition[] Definitions =
        {
            new("家具_床_天使床_01", "每晚躺下，就像睡进一片柔软的月光里"),
            new("家具_装饰_书柜_天使_01", "每翻开一本书，就像有一只蝴蝶翩翩飞出"),
            new("家具_竖琴_天使_01", "琴弦拨动的时候，有星星从缝隙里掉出来"),
            new("家具_装饰_盆栽_天使_02", "它把阳光卷进叶子，像兜着一篮碎金"),
            new("家具_装饰_窗台_天使_01", "窗户只需推开一条缝就能听见风窃窃、花私语。"),
            new("家具_床_恶魔床_01", "躺进被窝，连梦都能熬成甜的"),
            new("家具_装饰_镜子_恶魔_01", "魔镜魔镜，我是不是比天使更漂亮？"),
            new("家具_休闲_吉他_恶魔_01", "音乐响起的时候连窗外路过的萤火虫都跟着起舞"),
            new("家具_休闲_画架_恶魔_01", "落笔的时候，画里的花真的会盛开诶"),
            new("家具_装饰_储物的家具_恶魔_01", "它总说自己算是个迷你糖果屋")
        };

        [MenuItem("Tools/Gemini-Lab/Apartment/Setup Furniture Selection")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[ApartmentFurnitureSelectionAuthoring] 跳过 PlayMode 作者化。");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject? furnitureRoot = GameObject.Find(FurnitureRootName);
            if (furnitureRoot == null)
            {
                Debug.LogError("[ApartmentFurnitureSelectionAuthoring] 未找到 Furniture 根节点。");
                return;
            }


            Material? outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (outlineMaterial == null)
            {
                Debug.LogError($"[ApartmentFurnitureSelectionAuthoring] 缺少描边材质：{OutlineMaterialPath}");
                return;
            }

            GameObject presenterObject = FindOrCreateChild(furnitureRoot.transform, PresenterName);
            ApartmentFurnitureSelectionPresenter presenter = presenterObject.GetComponent<ApartmentFurnitureSelectionPresenter>()
                ?? presenterObject.AddComponent<ApartmentFurnitureSelectionPresenter>();

            var authoredEntries = new List<AuthoredEntry>();
            for (int i = 0; i < Definitions.Length; i++)
            {
                SelectionDefinition definition = Definitions[i];
                GameObject? target = ResolveTarget(definition.DefinitionId);
                if (target == null)
                {
                    Debug.LogWarning($"[ApartmentFurnitureSelectionAuthoring] 未找到家具目标，暂不创建选中项：{definition.DefinitionId}");
                    continue;
                }

                GameObject highlight = EnsureHighlight(target, outlineMaterial);
                authoredEntries.Add(new AuthoredEntry(target, highlight, definition.DefinitionId, definition.Message));
            }

            // Extend the authored list to visible furniture, without changing physics colliders.
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject target = renderer.gameObject;
                if (renderer.sprite == null || (!target.name.StartsWith("家具_", StringComparison.Ordinal) && target.GetComponent<FurniturePageLink>() == null) ||
                    target.GetComponent<Collider2D>() == null || authoredEntries.Exists(entry => entry.Target == target)) continue;
                string title = target.name.Replace("家具_", "").Replace("装饰_", "").Replace("休闲_", "").Replace("_01", "").Replace("_02", "").Replace("_", " · ");
                authoredEntries.Add(new AuthoredEntry(target, EnsureHighlight(target, outlineMaterial), target.name, title));
            }

            GameObject? viewportHost = GameObject.Find("ApartmentViewportHost");
            if (viewportHost == null)
            {
                Debug.LogError("[ApartmentFurnitureSelectionAuthoring] 未找到 ApartmentViewportHost。");
                return;
            }

            (GameObject messageRoot, TMP_Text messageText) = EnsureMessage(viewportHost);
            ConfigurePresenter(presenter, authoredEntries, messageRoot, messageText);
            RegisterWithViewportBridge(presenter);

            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int missingCount = 0;
            foreach (var definition in Definitions)
                if (!authoredEntries.Exists(entry => entry.DefinitionId == definition.DefinitionId)) missingCount++;
            Debug.Log($"[ApartmentFurnitureSelectionAuthoring] 完成家具选中作者化：{authoredEntries.Count} 项，缺失目标：{missingCount} 项。");
        }

        private static GameObject? ResolveTarget(string definitionId)
        {
            SceneFurnitureDefinitionHint[] hints = UnityEngine.Object.FindObjectsByType<SceneFurnitureDefinitionHint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < hints.Length; i++)
            {
                SceneFurnitureDefinitionHint hint = hints[i];
                if (hint != null && string.Equals(hint.DefinitionId, definitionId, StringComparison.Ordinal))
                {
                    return hint.gameObject;
                }
            }

            GameObject[] sceneObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (string.Equals(candidate.name, definitionId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                SpriteRenderer? renderer = candidate.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null &&
                    string.Equals(renderer.sprite.name, definitionId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject EnsureHighlight(GameObject target, Material outlineMaterial)
        {
            Transform? existing = target.transform.Find(HighlightName);
            GameObject highlight = existing != null ? existing.gameObject : new GameObject(HighlightName);
            if (existing == null)
            {
                highlight.transform.SetParent(target.transform, false);
            }

            SpriteRenderer source = target.GetComponent<SpriteRenderer>()
                ?? target.GetComponentInChildren<SpriteRenderer>(true)
                ?? throw new InvalidOperationException($"家具没有 SpriteRenderer：{target.name}");
            SpriteRenderer? renderer = highlight.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = highlight.AddComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                throw new InvalidOperationException($"无法在描边节点上创建 SpriteRenderer：{highlight.name}");
            }

            renderer.sprite = source.sprite;
            renderer.sharedMaterial = outlineMaterial;
            renderer.color = Color.white;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder - 1;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            MatchRendererTransform(target.transform, source.transform, highlight.transform);
            highlight.layer = target.layer;
            highlight.SetActive(false);
            return highlight;
        }

        private static void MatchRendererTransform(Transform target, Transform source, Transform highlight)
        {
            Vector3 targetLossyScale = target.lossyScale;
            Vector3 sourceLossyScale = source.lossyScale;
            highlight.localPosition = target.InverseTransformPoint(source.position);
            highlight.localRotation = Quaternion.Inverse(target.rotation) * source.rotation;
            highlight.localScale = new Vector3(
                SafeScaleRatio(sourceLossyScale.x, targetLossyScale.x),
                SafeScaleRatio(sourceLossyScale.y, targetLossyScale.y),
                SafeScaleRatio(sourceLossyScale.z, targetLossyScale.z));
        }

        private static float SafeScaleRatio(float sourceScale, float parentScale)
        {
            return Mathf.Abs(parentScale) < 0.0001f ? sourceScale : sourceScale / parentScale;
        }

        private static (GameObject Root, TMP_Text Text) EnsureMessage(GameObject viewportHost)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = viewportHost.layer;

            GameObject root = FindOrCreateChild(viewportHost.transform, MessageName);
            root.layer = uiLayer;
            RectTransform? rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = root.AddComponent<RectTransform>();
            }

            if (rootRect == null)
            {
                throw new InvalidOperationException($"无法在提示节点上创建 RectTransform：{root.name}");
            }
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 24f);
            rootRect.sizeDelta = new Vector2(820f, 92f);

            Image? background = root.GetComponent<Image>();
            if (background == null)
            {
                background = root.AddComponent<Image>();
            }

            if (background == null)
            {
                throw new InvalidOperationException($"无法在提示节点上创建 Image：{root.name}");
            }
            background.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
            Sprite? panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/ApartmentOnboarding/Panel.png");
            if (panelSprite != null)
            {
                background.sprite = panelSprite;
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = 4f;
                background.color = Color.white;
            }
            background.raycastTarget = false;

            GameObject textObject = FindOrCreateChild(root.transform, "Text");
            textObject.layer = uiLayer;
            RectTransform? textRect = textObject.GetComponent<RectTransform>();
            if (textRect == null)
            {
                textRect = textObject.AddComponent<RectTransform>();
            }

            if (textRect == null)
            {
                throw new InvalidOperationException($"无法在提示文本上创建 RectTransform：{textObject.name}");
            }
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 12f);
            textRect.offsetMax = new Vector2(-28f, -12f);

            TMP_Text? text = textObject.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            if (text == null)
            {
                throw new InvalidOperationException($"无法在提示文本上创建 TMP_Text：{textObject.name}");
            }
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 26f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            if (panelSprite != null) text.color = new Color(0.12f, 0.055f, 0.025f);
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            TMP_FontAsset? font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                text.font = font;
            }

            root.SetActive(false);
            return (root, text);
        }

        private static void ConfigurePresenter(
            ApartmentFurnitureSelectionPresenter presenter,
            List<AuthoredEntry> entries,
            GameObject messageRoot,
            TMP_Text messageText)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            SerializedProperty entryArray = serialized.FindProperty("_entries")
                ?? throw new InvalidOperationException("ApartmentFurnitureSelectionPresenter._entries not found.");
            entryArray.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty entry = entryArray.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_target").objectReferenceValue = entries[i].Target;
                entry.FindPropertyRelative("_highlight").objectReferenceValue = entries[i].Highlight;
                entry.FindPropertyRelative("_definitionId").stringValue = entries[i].DefinitionId;
                entry.FindPropertyRelative("_message").stringValue = entries[i].Message;
                entry.FindPropertyRelative("_hitRenderer").objectReferenceValue = entries[i].Target.GetComponent<SpriteRenderer>();
            }

            serialized.FindProperty("_messageRoot").objectReferenceValue = messageRoot;
            serialized.FindProperty("_messageText").objectReferenceValue = messageText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterWithViewportBridge(ApartmentFurnitureSelectionPresenter presenter)
        {
            ApartmentViewportInputBridge[] bridges = UnityEngine.Object.FindObjectsByType<ApartmentViewportInputBridge>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bridges.Length; i++)
            {
                ApartmentViewportInputBridge bridge = bridges[i];
                SerializedObject serialized = new SerializedObject(bridge);
                SerializedProperty handlers = serialized.FindProperty("_worldPointInteractables");
                if (handlers == null)
                {
                    continue;
                }

                bool alreadyRegistered = false;
                for (int j = 0; j < handlers.arraySize; j++)
                {
                    if (handlers.GetArrayElementAtIndex(j).objectReferenceValue == presenter)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (!alreadyRegistered)
                {
                    int index = handlers.arraySize;
                    handlers.InsertArrayElementAtIndex(index);
                    handlers.GetArrayElementAtIndex(index).objectReferenceValue = presenter;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private readonly struct SelectionDefinition
        {
            public SelectionDefinition(string definitionId, string message)
            {
                DefinitionId = definitionId;
                Message = message;
            }

            public string DefinitionId { get; }
            public string Message { get; }
        }

        private readonly struct AuthoredEntry
        {
            public AuthoredEntry(GameObject target, GameObject highlight, string definitionId, string message)
            {
                Target = target;
                Highlight = highlight;
                DefinitionId = definitionId;
                Message = message;
            }

            public GameObject Target { get; }
            public GameObject Highlight { get; }
            public string DefinitionId { get; }
            public string Message { get; }
        }
    }
}
#endif
