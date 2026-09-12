#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the outdoor tutorial UI without rebuilding any other WorldMap
    /// object. All page images and button sprites are saved in the scene so
    /// future art replacements only require changing Inspector references.
    /// </summary>
    public static class WorldMapOutdoorTutorialAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string ArtRoot = "Assets/_Project/Art/新手引导";
        private const string OutdoorArtRoot = ArtRoot + "/outdoor";
        private const string TutorialPanelName = "Panel_OutdoorTutorial";
        private const string OpenButtonName = "Btn_OutdoorTutorial";
        [MenuItem("Tools/Gemini-Lab/WorldMap/Author Outdoor Tutorial")]
        public static void Patch()
        {
            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject? canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[WorldMapOutdoorTutorialAuthoring] Canvas not found.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            int uiLayer = canvas.layer;

            GameObject panel = EnsureChild(canvas.transform, TutorialPanelName, uiLayer);
            ConfigureFullscreen(panel);

            SceneAuthoredImageVariantView pageView = GetOrAdd<SceneAuthoredImageVariantView>(panel);
            RemoveChild(panel.transform, "Page_Intro");
            GameObject[] pages = new GameObject[6];
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = EnsurePage(
                    panel.transform,
                    $"Page_Outdoor{i + 1}",
                    OutdoorArtRoot + $"/outdoor{i + 1}.png",
                    uiLayer);
            }

            ConfigurePageView(pageView, pages);

            Button previousButton = EnsureImageButton(
                panel.transform,
                "Btn_OutdoorTutorialPrevious",
                ArtRoot + "/left.png",
                uiLayer,
                new Vector2(0f, 0f),
                new Vector2(96f, 78f),
                new Vector2(166f, 67f));
            Button nextButton = EnsureImageButton(
                panel.transform,
                "Btn_OutdoorTutorialNext",
                ArtRoot + "/right.png",
                uiLayer,
                new Vector2(1f, 0f),
                new Vector2(-96f, 82f),
                new Vector2(154f, 140f));
            Button closeButton = EnsureImageButton(
                panel.transform,
                "Btn_OutdoorTutorialClose",
                OutdoorArtRoot + "/close.png",
                uiLayer,
                new Vector2(1f, 1f),
                new Vector2(-62f, -62f),
                new Vector2(117f, 141f));

            BindButton(previousButton, pageView.ShowPrevious);
            BindButton(nextButton, pageView.ShowNext);
            BindButton(closeButton, pageView.Hide);

            Button openButton = EnsurePlaceholderOpenButton(canvas.transform, uiLayer);
            BindButton(openButton, pageView.ShowPreview);

            // Page view owns the panel's active state. It starts closed while
            // the placeholder entry remains available in the outdoor scene.
            panel.SetActive(false);
            openButton.gameObject.SetActive(true);
            EditorUtility.SetDirty(pageView);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapOutdoorTutorialAuthoring] Outdoor tutorial authored with six pages and placeholder entry.");
        }

        private static void ConfigurePageView(
            SceneAuthoredImageVariantView pageView,
            GameObject[] pages)
        {
            SerializedObject serialized = new SerializedObject(pageView);
            serialized.Update();

            SerializedProperty? previewTarget = serialized.FindProperty("_previewTarget");
            if (previewTarget != null)
            {
                previewTarget.objectReferenceValue = pages.Length > 0 ? pages[0] : null;
            }

            SerializedProperty? previewKey = serialized.FindProperty("_previewKey");
            if (previewKey != null)
            {
                previewKey.stringValue = pages.Length > 0 ? "outdoor-1" : string.Empty;
            }

            SerializedProperty? variants = serialized.FindProperty("_variants");
            if (variants != null)
            {
                variants.arraySize = Mathf.Max(0, pages.Length - 1);
                for (int i = 1; i < pages.Length; i++)
                {
                    SerializedProperty element = variants.GetArrayElementAtIndex(i - 1);
                    element.FindPropertyRelative("Key").stringValue = $"outdoor-{i + 1}";
                    element.FindPropertyRelative("Target").objectReferenceValue = pages[i];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsurePage(Transform parent, string name, string spritePath, int layer)
        {
            GameObject page = EnsureChild(parent, name, layer);
            Image image = GetOrAdd<Image>(page);
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetRect(
                page.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1720f, 970f));
            page.transform.SetAsFirstSibling();
            return page;
        }

        private static Button EnsureImageButton(
            Transform parent,
            string name,
            string spritePath,
            int layer,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            GameObject buttonObject = EnsureChild(parent, name, layer);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchor, anchor, position, size);

            Image image = GetOrAdd<Image>(buttonObject);
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = GetOrAdd<Button>(buttonObject);
            button.targetGraphic = image;
            return button;
        }

        private static Button EnsurePlaceholderOpenButton(Transform canvas, int layer)
        {
            GameObject buttonObject = EnsureChild(canvas, OpenButtonName, layer);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(
                rect,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(150f, 62f),
                new Vector2(220f, 64f));

            Image image = GetOrAdd<Image>(buttonObject);
            image.color = new Color(0.18f, 0.26f, 0.38f, 0.96f);
            image.raycastTarget = true;

            Button button = GetOrAdd<Button>(buttonObject);
            button.targetGraphic = image;

            GameObject labelObject = EnsureChild(buttonObject.transform, "Label", layer);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            SetRect(labelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(labelObject);
            label.text = "新手指引";
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return button;
        }

        private static void BindButton(Button button, UnityAction action)
        {
            for (int index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, index);
            }
            button.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static GameObject EnsureChild(Transform parent, string name, int layer)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                existing.gameObject.layer = layer;
                return existing.gameObject;
            }

            GameObject created = new GameObject(name, typeof(RectTransform));
            created.layer = layer;
            created.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            return created;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }

        private static void ConfigureFullscreen(GameObject target)
        {
            SetRect(
                target.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            target.transform.localScale = Vector3.one;
            target.transform.SetAsLastSibling();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Sprite? LoadSprite(string path)
        {
            EnsureSpriteImporter(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[WorldMapOutdoorTutorialAuthoring] Sprite not found after import: {path}");
            }

            return sprite;
        }

        private static void EnsureSpriteImporter(string path)
        {
            TextureImporter? importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T? component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

    }
}
#endif
