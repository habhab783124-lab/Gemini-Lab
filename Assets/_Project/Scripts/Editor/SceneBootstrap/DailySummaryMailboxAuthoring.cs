#nullable enable
#if UNITY_EDITOR
using GeminiLab.Core.UI;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.HubUI.Panels;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 作者化 WorldMap 邮箱入口和 AI 每日小结。最终视觉节点与 Sprite 保存在场景中，
    /// 运行时面板只切换已有节点并填充每日数据。
    /// </summary>
    public static class DailySummaryMailboxAuthoring
    {
        private const string ApartmentScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string WorldMapScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string WorldMapRootName = "Canvas";
        private const string ButtonName = "MailboxButton";
        private const string PanelName = "Panel_DailySummaryMailbox";
        private const string WorldMapOpenTargetName = "WorldMapDailySummaryMailboxOpenTarget";
        private const string AiDiaryRoot = "Assets/_Project/Art/WorldMap/AI_diary/";

        [MenuItem("Tools/Gemini-Lab/Author Daily Summary Mailbox")]
        public static void Patch() => PatchWorldMap();

        [MenuItem("Tools/Gemini-Lab/Author WorldMap Daily Summary Mailbox")]
        public static void PatchWorldMap()
        {
            if (EditorSceneManager.GetActiveScene().path != WorldMapScenePath)
            {
                DisableApartmentLegacyEntry();
            }
            PatchScene(WorldMapScenePath, WorldMapRootName);
        }

        private static void DisableApartmentLegacyEntry()
        {
            if (!System.IO.File.Exists(ApartmentScenePath)) return;

            var scene = EditorSceneManager.GetActiveScene().path == ApartmentScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ApartmentScenePath, OpenSceneMode.Single);

            bool changed = false;
            foreach (string objectName in new[] { ButtonName, PanelName })
            {
                GameObject? legacyObject = GameObject.Find(objectName);
                if (legacyObject != null && legacyObject.activeSelf)
                {
                    legacyObject.SetActive(false);
                    EditorUtility.SetDirty(legacyObject);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void PatchScene(string scenePath, string rootName)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[DailySummaryMailboxAuthoring] 找不到场景：{scenePath}");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == scenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject? uiRoot = GameObject.Find(rootName);
            if (uiRoot == null)
            {
                Debug.LogError($"[DailySummaryMailboxAuthoring] 找不到 {rootName}，无法作者化每日小结。");
                return;
            }

            int uiLayer = uiRoot.layer;
            GameObject panel = EnsurePanel(uiRoot.transform, uiLayer);
            panel.transform.SetAsLastSibling();
            EnsureWorldMapMailboxBinding(uiRoot.transform, uiLayer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DailySummaryMailboxAuthoring] 每日小结已作者化到 {scenePath}。");
        }

        private static void EnsureWorldMapMailboxBinding(Transform canvas, int uiLayer)
        {
            GameObject? mailbox = GameObject.Find("邮箱");
            if (mailbox == null)
            {
                Debug.LogError("[DailySummaryMailboxAuthoring] WorldMap 找不到室外邮箱对象。");
                return;
            }

            Transform? targetTransform = canvas.Find(WorldMapOpenTargetName);
            GameObject target = targetTransform != null ? targetTransform.gameObject : new GameObject(WorldMapOpenTargetName);
            target.transform.SetParent(canvas, false);
            target.layer = uiLayer;
            target.SetActive(false);

            var openButton = target.GetComponent<PanelOpenButton>() ?? target.AddComponent<PanelOpenButton>();
            SetPanelId(openButton);

            var clickable = mailbox.GetComponent<ClickableSceneObject>() ?? mailbox.AddComponent<ClickableSceneObject>();
            for (int index = clickable.OnClicked.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(clickable.OnClicked, index);
            }
            UnityEventTools.AddPersistentListener(clickable.OnClicked, openButton.OnClick);
            EditorUtility.SetDirty(clickable);
        }

        private static void SetPanelId(PanelOpenButton openButton)
        {
            var openButtonSo = new SerializedObject(openButton);
            var panelId = openButtonSo.FindProperty("_panelId");
            if (panelId != null)
            {
                for (int index = 0; index < panelId.enumNames.Length; index++)
                {
                    if (panelId.enumNames[index] == nameof(PanelId.DailySummaryMailbox))
                    {
                        panelId.intValue = (int)PanelId.DailySummaryMailbox;
                        break;
                    }
                }
            }
            openButtonSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsurePanel(Transform parent, int uiLayer)
        {
            GameObject panel = EnsureRectChild(parent, PanelName, uiLayer);
            var panelRt = panel.GetComponent<RectTransform>()!;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            GameObject content = EnsureRectChild(panel.transform, "DailySummaryContent", uiLayer);
            var contentRt = content.GetComponent<RectTransform>()!;
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(1440f, 1046f);
            var contentImage = content.GetComponent<Image>() ?? content.AddComponent<Image>();
            contentImage.sprite = LoadSprite("background.png");
            contentImage.color = Color.white;
            contentImage.preserveAspect = true;
            contentImage.raycastTarget = true;

            TMP_Text title = EnsureText(content.transform, "Title", "AI每日小结", new Vector2(-40f, 438f), new Vector2(620f, 64f), 34f, TextAlignmentOptions.Center);
            title.color = new Color(0.31f, 0.19f, 0.11f, 1f);

            // 日期列表由 Scene 中预先作者化的 ScrollRect 和固定数量日期项组成。
            // 运行时只切换项的 selected/unselected 子节点和日期文字，不创建 UI。
            RemoveChildIfExists(content.transform, "Date");
            RemoveChildIfExists(content.transform, "InputText");
            RemoveChildIfExists(content.transform, "DateButton");
            RemoveChildIfExists(content.transform, "InputButton");
            GameObject dateViewport = EnsureRectChild(content.transform, "DateListViewport", uiLayer);
            ConfigureRect(dateViewport, new Vector2(-505f, -18f), new Vector2(292f, 730f));
            var viewportImage = dateViewport.GetComponent<Image>() ?? dateViewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            _ = dateViewport.GetComponent<RectMask2D>() ?? dateViewport.AddComponent<RectMask2D>();

            GameObject dateList = EnsureRectChild(dateViewport.transform, "DateListContent", uiLayer);
            var dateListRt = dateList.GetComponent<RectTransform>()!;
            dateListRt.anchorMin = new Vector2(0f, 1f);
            dateListRt.anchorMax = new Vector2(1f, 1f);
            dateListRt.pivot = new Vector2(0.5f, 1f);
            dateListRt.anchoredPosition = Vector2.zero;
            dateListRt.sizeDelta = Vector2.zero;
            var layout = dateList.GetComponent<VerticalLayoutGroup>() ?? dateList.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = dateList.GetComponent<ContentSizeFitter>() ?? dateList.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = dateViewport.GetComponent<ScrollRect>() ?? dateViewport.AddComponent<ScrollRect>();
            scroll.viewport = dateViewport.GetComponent<RectTransform>();
            scroll.content = dateListRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42f;

            var dateOptions = new DailySummaryDateOption[14];
            for (int index = 0; index < dateOptions.Length; index++)
            {
                string optionName = index == 0 ? "DateButton" : index == 1 ? "InputButton" : $"DateOption_{index + 1:00}";
                dateOptions[index] = EnsureDateOption(dateList.transform, optionName, uiLayer);
            }

            TMP_Text summary = EnsureText(content.transform, "SummaryText", "---", new Vector2(-245f, -270f), new Vector2(420f, 120f), 22f, TextAlignmentOptions.Center);
            summary.color = new Color(0.31f, 0.19f, 0.11f, 1f);
            TMP_Text angel = EnsureText(content.transform, "AngelNoteText", "---", new Vector2(-225f, 88f), new Vector2(270f, 84f), 18f, TextAlignmentOptions.Center);
            angel.color = new Color(0.31f, 0.19f, 0.11f, 1f);
            TMP_Text devil = EnsureText(content.transform, "DevilNoteText", "---", new Vector2(282f, 74f), new Vector2(190f, 110f), 18f, TextAlignmentOptions.Center);
            devil.color = new Color(0.31f, 0.19f, 0.11f, 1f);

            Button closeButton = EnsureButton(content.transform, "CloseButton", LoadSprite("close.png"), new Vector2(610f, 428f), new Vector2(88f, 88f), uiLayer);
            RemoveChildIfExists(closeButton.transform, "Label");
            Button angelNoteButton = EnsureButton(content.transform, "AngelNoteButton", LoadSprite("angel_note.png"), new Vector2(-225f, 88f), new Vector2(317f, 170f), uiLayer);
            Button summaryButton = EnsureButton(content.transform, "SummaryButton", LoadSprite("summary.png"), new Vector2(-245f, -270f), new Vector2(262f, 215f), uiLayer);
            Button devilNoteButton = EnsureButton(content.transform, "DevilNoteButton", LoadSprite("devil_note.png"), new Vector2(282f, 74f), new Vector2(233f, 240f), uiLayer);
            Button angelCardButton = EnsureButton(content.transform, "AngelCardButton", LoadSprite("angel_card.png"), new Vector2(270f, -220f), new Vector2(150f, 91f), uiLayer);
            Button devilCardButton = EnsureButton(content.transform, "DevilCardButton", LoadSprite("devil_card.png"), new Vector2(450f, -220f), new Vector2(153f, 93f), uiLayer);
            Button popupButton = EnsureButton(content.transform, "PopupButton", LoadSprite("弹窗.png"), new Vector2(470f, 150f), new Vector2(260f, 260f), uiLayer);

            // Button images are the authored paper/card visuals; dynamic text remains a
            // separate raycast-free child so the runtime can fill the daily data.
            angelNoteButton.transform.SetAsFirstSibling();
            summaryButton.transform.SetAsFirstSibling();
            devilNoteButton.transform.SetAsFirstSibling();
            angelCardButton.transform.SetAsFirstSibling();
            devilCardButton.transform.SetAsFirstSibling();
            popupButton.transform.SetAsFirstSibling();

            DailySummaryDetailPopup popup = EnsureDetailPopup(content.transform, uiLayer);
            var panelComponent = panel.GetComponent<DailySummaryMailboxPanel>() ?? panel.AddComponent<DailySummaryMailboxPanel>();
            var panelSo = new SerializedObject(panelComponent);
            SetObject(panelSo, "_content", content);
            SetObject(panelSo, "_closeButton", closeButton);
            SetObjectArray(panelSo, "_dateOptions", dateOptions);
            SetObject(panelSo, "_summaryText", summary);
            SetObject(panelSo, "_angelNoteText", angel);
            SetObject(panelSo, "_devilNoteText", devil);
            SetObject(panelSo, "_angelNoteButton", angelNoteButton);
            SetObject(panelSo, "_summaryButton", summaryButton);
            SetObject(panelSo, "_devilNoteButton", devilNoteButton);
            SetObject(panelSo, "_angelCardButton", angelCardButton);
            SetObject(panelSo, "_devilCardButton", devilCardButton);
            SetObject(panelSo, "_popupButton", popupButton);
            SetObject(panelSo, "_detailPopup", popup);
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);
            return panel;
        }

        private static DailySummaryDetailPopup EnsureDetailPopup(Transform parent, int uiLayer)
        {
            GameObject root = EnsureRectChild(parent, "DailySummaryDetailPopup", uiLayer);
            var rootRt = root.GetComponent<RectTransform>()!;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            GameObject backdrop = EnsureRectChild(root.transform, "PopupBackdrop", uiLayer);
            var backdropRt = backdrop.GetComponent<RectTransform>()!;
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            var backdropImage = backdrop.GetComponent<Image>() ?? backdrop.AddComponent<Image>();
            backdropImage.color = new Color(0.06f, 0.04f, 0.03f, 0.70f);
            backdropImage.raycastTarget = true;
            Button backdropButton = backdrop.GetComponent<Button>() ?? backdrop.AddComponent<Button>();

            GameObject popupContent = EnsureRectChild(root.transform, "PopupContent", uiLayer);
            var popupContentRt = popupContent.GetComponent<RectTransform>()!;
            popupContentRt.anchorMin = new Vector2(0.5f, 0.5f);
            popupContentRt.anchorMax = new Vector2(0.5f, 0.5f);
            popupContentRt.pivot = new Vector2(0.5f, 0.5f);
            popupContentRt.anchoredPosition = Vector2.zero;
            popupContentRt.sizeDelta = new Vector2(900f, 760f);
            var popupImage = popupContent.GetComponent<Image>() ?? popupContent.AddComponent<Image>();
            // 详情页不再铺一层纯色底，只保留弹窗资源本身；外层 backdrop 仍负责模态遮罩和点外关闭。
            popupImage.sprite = null;
            popupImage.color = Color.clear;
            popupImage.raycastTarget = false;
            popupImage.enabled = false;

            GameObject angelView = EnsureSpriteView(popupContent.transform, "AngelNoteView", "angel_note.png", Vector2.zero, new Vector2(650f, 360f), uiLayer);
            GameObject summaryView = EnsureSpriteView(popupContent.transform, "SummaryView", "summary.png", Vector2.zero, new Vector2(520f, 430f), uiLayer);
            GameObject devilView = EnsureSpriteView(popupContent.transform, "DevilNoteView", "devil_note.png", Vector2.zero, new Vector2(430f, 520f), uiLayer);
            GameObject angelCardView = EnsureSpriteView(popupContent.transform, "AngelCardView", "angel_card.png", Vector2.zero, new Vector2(540f, 328f), uiLayer);
            GameObject devilCardView = EnsureSpriteView(popupContent.transform, "DevilCardView", "devil_card.png", Vector2.zero, new Vector2(540f, 328f), uiLayer);
            GameObject popupView = EnsureSpriteView(popupContent.transform, "PopupView", "弹窗1.png", Vector2.zero, new Vector2(760f, 760f), uiLayer);

            TMP_Text title = EnsureText(popupContent.transform, "PopupTitleText", "详情", new Vector2(0f, 320f), new Vector2(680f, 64f), 30f, TextAlignmentOptions.Center);
            title.color = new Color(0.31f, 0.19f, 0.11f, 1f);
            // 正文必须叠加在放大的便签/总结纸张内部，而不是放在弹窗底部。
            TMP_Text body = EnsureText(popupContent.transform, "PopupBodyText", "", new Vector2(0f, -30f), new Vector2(400f, 260f), 22f, TextAlignmentOptions.Center);
            body.color = new Color(0.31f, 0.19f, 0.11f, 1f);
            Button close = EnsureButton(popupContent.transform, "PopupCloseButton", LoadSprite("close.png"), new Vector2(385f, 315f), new Vector2(76f, 76f), uiLayer);
            RemoveChildIfExists(close.transform, "Label");
            body.gameObject.SetActive(true);
            body.transform.SetAsLastSibling();

            var component = root.GetComponent<DailySummaryDetailPopup>() ?? root.AddComponent<DailySummaryDetailPopup>();
            var so = new SerializedObject(component);
            SetObject(so, "_popupRoot", root);
            SetObject(so, "_angelNoteView", angelView);
            SetObject(so, "_summaryView", summaryView);
            SetObject(so, "_devilNoteView", devilView);
            SetObject(so, "_angelCardView", angelCardView);
            SetObject(so, "_devilCardView", devilCardView);
            SetObject(so, "_popupView", popupView);
            SetObject(so, "_closeButton", close);
            SetObject(so, "_backdropButton", backdropButton);
            SetObject(so, "_titleText", title);
            SetObject(so, "_bodyText", body);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return component;
        }

        private static DailySummaryDateOption EnsureDateOption(Transform parent, string name, int uiLayer)
        {
            GameObject option = EnsureRectChild(parent, name, uiLayer);
            ConfigureRect(option, Vector2.zero, new Vector2(292f, 85f));

            var rootImage = option.GetComponent<Image>() ?? option.AddComponent<Image>();
            rootImage.sprite = null;
            rootImage.color = Color.clear;
            rootImage.raycastTarget = true;
            var button = option.GetComponent<Button>() ?? option.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = rootImage;

            GameObject selected = EnsureRectChild(option.transform, "SelectedVisual", uiLayer);
            ConfigureRect(selected, Vector2.zero, new Vector2(292f, 85f));
            var selectedImage = selected.GetComponent<Image>() ?? selected.AddComponent<Image>();
            selectedImage.sprite = LoadSprite("selected.png");
            selectedImage.color = Color.white;
            selectedImage.preserveAspect = true;
            selectedImage.raycastTarget = false;

            GameObject unselected = EnsureRectChild(option.transform, "UnselectedVisual", uiLayer);
            ConfigureRect(unselected, Vector2.zero, new Vector2(282f, 79f));
            var unselectedImage = unselected.GetComponent<Image>() ?? unselected.AddComponent<Image>();
            unselectedImage.sprite = LoadSprite("unselected.png");
            unselectedImage.color = Color.white;
            unselectedImage.preserveAspect = true;
            unselectedImage.raycastTarget = false;

            TMP_Text dateText = EnsureText(option.transform, "DateLabel", "日期", Vector2.zero, new Vector2(250f, 52f), 22f, TextAlignmentOptions.Center);
            dateText.color = new Color(0.31f, 0.19f, 0.11f, 1f);
            dateText.raycastTarget = false;

            selected.transform.SetAsFirstSibling();
            unselected.transform.SetAsFirstSibling();
            dateText.transform.SetAsLastSibling();
            selected.SetActive(true);
            unselected.SetActive(false);

            var optionComponent = option.GetComponent<DailySummaryDateOption>() ?? option.AddComponent<DailySummaryDateOption>();
            var so = new SerializedObject(optionComponent);
            SetObject(so, "_button", button);
            SetObject(so, "_dateText", dateText);
            SetObject(so, "_selectedVisual", selected);
            SetObject(so, "_unselectedVisual", unselected);
            so.ApplyModifiedPropertiesWithoutUndo();
            option.SetActive(true);
            return optionComponent;
        }

        private static void ConfigureRect(GameObject target, Vector2 position, Vector2 size)
        {
            var rt = target.GetComponent<RectTransform>()!;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static GameObject EnsureSpriteView(Transform parent, string name, string assetName, Vector2 position, Vector2 size, int uiLayer)
        {
            GameObject view = EnsureRectChild(parent, name, uiLayer);
            var rt = view.GetComponent<RectTransform>()!;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = view.GetComponent<Image>() ?? view.AddComponent<Image>();
            image.sprite = LoadSprite(assetName);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            view.SetActive(false);
            return view;
        }

        private static Button EnsureButton(Transform parent, string name, Sprite? sprite, Vector2 position, Vector2 size, int uiLayer)
        {
            GameObject buttonGo = EnsureRectChild(parent, name, uiLayer);
            var rt = buttonGo.GetComponent<RectTransform>()!;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = buttonGo.GetComponent<Image>() ?? buttonGo.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = buttonGo.GetComponent<Button>() ?? buttonGo.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return button;
        }

        private static GameObject EnsureRectChild(Transform parent, string name, int uiLayer)
        {
            Transform? existing = parent.Find(name);
            if (existing != null && existing.GetComponent<RectTransform>() == null)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }

            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            go.GetComponent<RectTransform>()!.localScale = Vector3.one;
            return go;
        }

        private static TMP_Text EnsureText(Transform parent, string name, string defaultText, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = EnsureRectChild(parent, name, parent.gameObject.layer);
            var rt = go.GetComponent<RectTransform>()!;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static void RemoveChildIfExists(Transform parent, string name)
        {
            Transform? child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static Sprite? LoadSprite(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(AiDiaryRoot + fileName);

        private static void SetObject(SerializedObject serialized, string propertyName, Object? value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray) return;

            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
#endif
