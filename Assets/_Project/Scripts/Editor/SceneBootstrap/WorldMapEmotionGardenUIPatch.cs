#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.DevTools;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.HubUI.Panels;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 WorldMap 场景的 Canvas 下创建情绪花园三个面板 + 触发按钮。
    /// 幂等：已存在的对象只补缺失组件和连线，不覆盖用户手动调整的属性。
    /// </summary>
    public static class WorldMapEmotionGardenUIPatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string FlowerCodexArtDir = "Assets/_Project/Art/WorldMap/UI/flowerCodex";
        private const string FlowerInfoArtDir = "Assets/_Project/Art/WorldMap/UI/flower_info";
        private const string FlowerArtDir = "Assets/_Project/Art/WorldMap/flower";
        private const string FlowerHeadArtDir = "Assets/_Project/Art/WorldMap/花朵图鉴/花朵";
        private const string FlowerArtCatalogPath = "Assets/_Project/Art/WorldMap/flower/EmotionFlowerArtCatalog.asset";
        private const string EmotionInputArtDir = "Assets/_Project/Art/WorldMap/输入心情";
        private const string WeeklyOutlineShaderName = "GeminiLab/UI/SpriteAlphaOutline";
        private const string WeeklyOutlineMaterialPath = "Assets/_Project/Art/WorldMap/UI/garden_week/SelectedBottleOutline.mat";
        private const int FirstCodexFlowerNumber = 27;

        private static readonly string[] FlowerArtEmotionTypes =
        {
            "喜悦", "悲伤", "愤怒", "平静", "爱", "恐惧", "惊讶", "期待", "孤独"
        };

        // 以完整花图的透明边界为基准的初始土壤位置。
        // 这些值只负责首次作者化，之后仍可在 Scene/Inspector 中逐花调整。
        private static readonly Dictionary<string, float> DetailSoilYByFlower = new(System.StringComparer.Ordinal)
        {
            ["angel|喜悦"] = -173.4f,
            ["demon|喜悦"] = -173.4f,
            ["angel|悲伤"] = -158.3f,
            ["demon|悲伤"] = -161.6f,
            ["angel|愤怒"] = -164.9f,
            ["demon|愤怒"] = -164.9f,
            ["angel|平静"] = -164.2f,
            ["demon|平静"] = -165.5f,
            ["angel|爱"] = -164.2f,
            ["demon|爱"] = -165.5f,
            ["angel|恐惧"] = -164.9f,
            ["demon|恐惧"] = -162.9f,
            ["angel|惊讶"] = -163.5f,
            ["demon|惊讶"] = -164.2f,
            ["angel|期待"] = -163.5f,
            ["demon|期待"] = -163.5f,
            ["angel|孤独"] = -164.2f
        };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[WorldMapEmotionGardenUI] 未找到 Canvas，请先跑 Author WorldMap Scene");
                return;
            }

            int uiLayer = canvasGo.layer;

            var panelInput = EnsurePanel<EmotionInputPanelStub>(canvasGo, uiLayer, "Panel_EmotionInput");
            var panelWeekly = EnsurePanel<WeeklyGardenPanelStub>(canvasGo, uiLayer, "Panel_WeeklyGarden");
            var panelCollection = EnsurePanel<FlowerCollectionPanelStub>(canvasGo, uiLayer, "Panel_EmotionCollection");

            SetupEmotionInputContentReference(panelInput, uiLayer);
            SetupWeeklyGardenContent(panelWeekly, uiLayer);
            SetupFlowerCollectionBookContent(panelCollection, uiLayer);

            EnsureDevTools(canvasGo, uiLayer);

            var inputBtn = EnsureButton(canvasGo, uiLayer, "Btn_EmotionInput", "情绪输入", 0);
            var weeklyBtn = EnsureButton(canvasGo, uiLayer, "Btn_WeeklyGarden", "每周培育", 1);
            var collectionBtn = EnsureButton(canvasGo, uiLayer, "Btn_EmotionCollection", "情绪图鉴", 2);

            WireButtonToPanel(inputBtn, PanelId.EmotionInput);
            WireButtonToPanel(weeklyBtn, PanelId.WeeklyGardenView);
            WireButtonToPanel(collectionBtn, PanelId.EmotionCollection);

            // 7/20 修改清单
            EnsureArrowButtons(canvasGo, uiLayer);
            EnsureCabinReturnPortal();
            WorldMapInteractiveObjectAuthoring.Patch();
            HideGardenPlots();
            DisableCameraFollow();
            WorldMapFlowerPlacementAuthoring.Patch(canvasGo, uiLayer);
            WorldMapOutdoorPetAnimationAuthoring.Patch();
            WorldMapDayNightAuthoring.Patch();
            WorldMapPetAnimationTriggerAuthoring.Patch();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapEmotionGardenUI] 情绪花园面板 + 触发按钮 + 7/20 修改已应用到 WorldMap Canvas");
        }

        // ── DevTools 调试工具父节点 ──────────────────────────

        /// <summary>
        /// 只作者化输入心情面板，避免为了替换一套 UI 而重跑其它 WorldMap 补丁。
        /// </summary>
        public static void PatchEmotionInputArtOnly()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[WorldMapEmotionGardenUI] 未找到 Canvas，无法作者化输入心情面板");
                return;
            }

            int uiLayer = canvasGo.layer;
            var panelInput = EnsurePanel<EmotionInputPanelStub>(canvasGo, uiLayer, "Panel_EmotionInput");
            SetupEmotionInputContentReference(panelInput, uiLayer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapEmotionGardenUI] 输入心情面板已按天使/恶魔参考图作者化");
        }

        private static void EnsureDevTools(GameObject canvasGo, int uiLayer)
        {
            var devTools = canvasGo.transform.Find("DevTools");
            if (devTools == null)
            {
                var go = new GameObject("DevTools");
                go.transform.SetParent(canvasGo.transform, false);
                go.layer = uiLayer;
                devTools = go.transform;
            }

            devTools.gameObject.SetActive(DevMode.Active);

            var actions = devTools.GetComponent<DevToolsActions>();
            if (actions == null) actions = devTools.gameObject.AddComponent<DevToolsActions>();

            // 清理旧版散落在面板里的调试按钮
            foreach (var panelName in new[] { "Panel_EmotionInput", "Panel_EmotionCollection" })
            {
                var panel = canvasGo.transform.Find(panelName);
                if (panel == null) continue;
                foreach (var oldName in new[] { "DebugResetBtn", "DebugNextDayBtn", "DebugBloomBtn" })
                {
                    var old = panel.Find("Content")?.Find(oldName);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
            }

            EnsureDevButton(devTools, uiLayer, "DebugNextDayBtn", "进入下一天(调试)",
                new Vector2(-32, -180), actions.AdvanceDay);
            EnsureDevButton(devTools, uiLayer, "ResetClockBtn", "重置时钟(调试)",
                new Vector2(-32, -224), actions.ResetClock);
            EnsureDevButton(devTools, uiLayer, "ClearGardenBtn", "清空花园数据(调试)",
                new Vector2(-32, -268), actions.ClearGardenData);
        }

        private static GameObject EnsureDevButton(Transform parent, int uiLayer, string name, string label,
            Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = EnsureChild(parent, name, uiLayer);
            if (btnGo.transform.childCount != 0) return btnGo;

            var drt = GetOrAdd<RectTransform>(btnGo);
            drt.anchorMin = new Vector2(1f, 1f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(1f, 1f);
            drt.anchoredPosition = anchoredPosition;
            drt.sizeDelta = new Vector2(180, 36);
            var dImg = GetOrAdd<Image>(btnGo);
            dImg.color = new Color(0.6f, 0.25f, 0.1f, 0.9f);
            var btn = GetOrAdd<Button>(btnGo);

            var labelGo = EnsureChild(btnGo.transform, "Label", uiLayer);
            var lrt = GetOrAdd<RectTransform>(labelGo);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = GetOrAdd<TextMeshProUGUI>(labelGo);
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 14;
            tmp.color = Color.white;

            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return btnGo;
        }

        // ── 通用 panel / button ──────────────────────────────

        private static GameObject EnsurePanel<T>(GameObject canvas, int uiLayer, string name) where T : MonoBehaviour
        {
            var existing = canvas.transform.Find(name);
            if (existing != null)
            {
                if (existing.GetComponent<T>() == null)
                    existing.gameObject.AddComponent<T>();
                return existing.gameObject;
            }
            return CreateStubPanel<T>(canvas, uiLayer, name);
        }

        private static Button EnsureButton(GameObject canvas, int uiLayer, string name, string label, int index)
        {
            var existing = canvas.transform.Find(name);
            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                return existingBtn != null ? existingBtn : existing.gameObject.AddComponent<Button>();
            }
            return CreatePanelButton(canvas, uiLayer, name, label, index);
        }

        private static void WireButtonToPanel(Button btn, PanelId panelId)
        {
            var panelBtn = btn.GetComponent<PanelOpenButton>();
            if (panelBtn == null) panelBtn = btn.gameObject.AddComponent<PanelOpenButton>();

            var so = new SerializedObject(panelBtn);
            so.FindProperty("_panelId").intValue = (int)panelId;
            so.ApplyModifiedProperties();
        }

        // ── 基础面板骨架（Content + Close + Balance）─────────

        private static GameObject CreateStubPanel<T>(GameObject canvas, int uiLayer, string name) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.layer = uiLayer;
            var crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var img = contentGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            // Close button
            var closeBtnGo = new GameObject("Btn_Close");
            closeBtnGo.transform.SetParent(contentGo.transform, false);
            closeBtnGo.layer = uiLayer;
            var cbrt = closeBtnGo.AddComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(1f, 1f);
            cbrt.anchorMax = new Vector2(1f, 1f);
            cbrt.pivot = new Vector2(1f, 1f);
            cbrt.anchoredPosition = new Vector2(-24, -24);
            cbrt.sizeDelta = new Vector2(40, 40);
            var cbImg = closeBtnGo.AddComponent<Image>();
            cbImg.color = new Color(1f, 1f, 1f, 0.15f);
            var closeBtn = closeBtnGo.AddComponent<Button>();

            var xGo = new GameObject("X");
            xGo.transform.SetParent(closeBtnGo.transform, false);
            xGo.layer = uiLayer;
            var xrt = xGo.AddComponent<RectTransform>();
            xrt.anchorMin = Vector2.zero; xrt.anchorMax = Vector2.one;
            xrt.offsetMin = Vector2.zero; xrt.offsetMax = Vector2.zero;
            var xtmp = xGo.AddComponent<TextMeshProUGUI>();
            xtmp.text = "✕";
            xtmp.alignment = TextAlignmentOptions.Center;
            xtmp.fontSize = 22;
            xtmp.color = Color.white;

            // Coin balance
            var topResGo = new GameObject("TopResource");
            topResGo.transform.SetParent(contentGo.transform, false);
            topResGo.layer = uiLayer;
            var trRt = topResGo.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.5f, 0.5f);
            trRt.anchorMax = new Vector2(0.5f, 0.5f);
            trRt.pivot = new Vector2(0.5f, 0.5f);
            trRt.anchoredPosition = new Vector2(582, 442);
            trRt.sizeDelta = new Vector2(295, 91);
            var trImg = topResGo.AddComponent<Image>();
            trImg.color = Color.white;

            var balanceGo = new GameObject("BalanceLabel");
            balanceGo.transform.SetParent(topResGo.transform, false);
            balanceGo.layer = uiLayer;
            var bRt = balanceGo.AddComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(-16, -4);
            var bTmp = balanceGo.AddComponent<TextMeshProUGUI>();
            bTmp.text = "0";
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.fontSize = 20;
            bTmp.color = new Color(1f, 0.84f, 0f, 1f);
            balanceGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
            balanceGo.AddComponent<CoinBalanceDisplay>();

            var stub = go.AddComponent<T>();
            var so = new SerializedObject(stub);
            so.FindProperty("_content").objectReferenceValue = contentGo;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("_balanceText").objectReferenceValue = bTmp;
            so.ApplyModifiedProperties();

            // Content 初始保持 active，方便在 Scene 视图中手动编辑 UI。
            // 运行时由 StubPanelBase.Awake() 负责关闭，Open 时再打开。
            return go;
        }

        // ── 情绪输入面板内容 ──────────────────────────────────

        private static void SetupEmotionInputContentReference(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<EmotionInputPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;
            var contentImage = GetOrAdd<Image>(content);
            contentImage.sprite = null;
            contentImage.color = new Color(1f, 1f, 1f, 0f);
            // Content 是模态面板的全屏透明阻挡层：即使鼠标落在主题图片之外，
            // 点击也不能穿透到 WorldMap 的“室内”等场景物体；面板关闭时由 StubPanelBase 一起隐藏。
            contentImage.raycastTarget = true;

            // 两套主题都预先存在于 Scene，运行时只切换 active 状态，不创建最终视觉节点。
            var visualRoot = EnsureFullRect(contentT, "EmotionInputVisual", uiLayer);
            visualRoot.transform.SetAsFirstSibling();
            var angelTheme = EnsureFullRect(visualRoot.transform, "AngelTheme", uiLayer);
            var demonTheme = EnsureFullRect(visualRoot.transform, "DemonTheme", uiLayer);
            angelTheme.SetActive(true);
            demonTheme.SetActive(false);

            var angelBackground = EnsureImageChild(angelTheme.transform, "Background", uiLayer,
                LoadEmotionInputSprite("angel_Input", "background"), Vector2.zero, new Vector2(1175f, 743f));
            ApplyRect(angelBackground.gameObject, Vector2.zero, new Vector2(1175f, 743f));
            var demonBackground = EnsureImageChild(demonTheme.transform, "Background", uiLayer,
                LoadEmotionInputSprite("devil_Input", "background"), Vector2.zero, new Vector2(1575f, 799f));
            ApplyRect(demonBackground.gameObject, Vector2.zero, new Vector2(1575f, 799f));

            // 员工卡就是参考图中的培育者选择入口；点击当前卡片即可切换到另一位桌宠。
            var angelOwnerButton = EnsureImageButton(angelTheme.transform, "OwnerCardButton", uiLayer, null,
                new Vector2(474f, -12f), new Vector2(235f, 340f));
            ConfigureTransparentButton(angelOwnerButton);
            var demonOwnerButton = EnsureImageButton(demonTheme.transform, "OwnerCardButton", uiLayer, null,
                new Vector2(-638f, -24f), new Vector2(275f, 420f));
            ConfigureTransparentButton(demonOwnerButton);

            var inputField = EnsureInputField(contentT, uiLayer);
            ApplyRect(inputField.gameObject, new Vector2(0f, 42f), new Vector2(708f, 239f));
            ConfigureEmotionInputField(inputField);

            var angelInput = EnsureImageChild(angelTheme.transform, "InputVisual", uiLayer,
                LoadEmotionInputSprite("angel_Input", "input"), new Vector2(0f, 42f), new Vector2(708f, 239f));
            ApplyRect(angelInput.gameObject, new Vector2(0f, 42f), new Vector2(708f, 239f));
            var demonInput = EnsureImageChild(demonTheme.transform, "InputVisual", uiLayer,
                LoadEmotionInputSprite("devil_Input", "input"), new Vector2(0f, 42f), new Vector2(713f, 253f));
            ApplyRect(demonInput.gameObject, new Vector2(0f, 42f), new Vector2(713f, 253f));

            var angelSubmit = EnsureImageChild(angelTheme.transform, "SubmitVisual", uiLayer,
                LoadEmotionInputSprite("angel_Input", "submit"), new Vector2(0f, -222f), new Vector2(446f, 98f));
            ApplyRect(angelSubmit.gameObject, new Vector2(0f, -222f), new Vector2(446f, 98f));
            var demonSubmit = EnsureImageChild(demonTheme.transform, "SubmitVisual", uiLayer,
                LoadEmotionInputSprite("devil_Input", "submit"), new Vector2(0f, -222f), new Vector2(488f, 110f));
            ApplyRect(demonSubmit.gameObject, new Vector2(0f, -222f), new Vector2(488f, 110f));

            var submitBtn = EnsureButtonWithLabel(contentT, uiLayer, "SubmitBtn", string.Empty,
                new Vector2(0f, -222f), new Vector2(488f, 110f), new Color(1f, 1f, 1f, 0f));
            ApplyRect(submitBtn.gameObject, new Vector2(0f, -222f), new Vector2(488f, 110f));
            ConfigureTransparentButton(submitBtn);
            var submitLabel = submitBtn.transform.Find("Label");
            if (submitLabel != null) submitLabel.gameObject.SetActive(false);

            var ownerGo = EnsureChildText(contentT, uiLayer, "OwnerText", string.Empty, 20,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(300, 36));
            ownerGo.SetActive(false);
            var titleGo = EnsureChildText(contentT, uiLayer, "Title_Input", string.Empty, 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(600, 46));
            titleGo.SetActive(false);

            var statusGo = EnsureChildText(contentT, uiLayer, "StatusText", string.Empty, 18,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -315), new Vector2(700, 34));
            var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
            statusTmp.color = new Color(0.38f, 0.23f, 0.16f, 1f);

            foreach (var oldName in new[] { "DebugResetBtn", "DebugNextDayBtn" })
            {
                var old = contentT.Find(oldName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            so.FindProperty("_inputField").objectReferenceValue = inputField;
            so.FindProperty("_submitButton").objectReferenceValue = submitBtn;
            so.FindProperty("_statusText").objectReferenceValue = statusTmp;
            so.FindProperty("_ownerText").objectReferenceValue = ownerGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_angelTheme").objectReferenceValue = angelTheme;
            so.FindProperty("_demonTheme").objectReferenceValue = demonTheme;
            so.FindProperty("_angelOwnerButton").objectReferenceValue = angelOwnerButton;
            so.FindProperty("_demonOwnerButton").objectReferenceValue = demonOwnerButton;
            so.ApplyModifiedProperties();
        }

        private static Sprite? LoadEmotionInputSprite(string ownerFolder, string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{EmotionInputArtDir}/{ownerFolder}/{fileName}.png");
        }

        private static void ConfigureTransparentButton(Button button)
        {
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic is Image image)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
                image.preserveAspect = false;
            }
        }

        private static void ConfigureEmotionInputField(TMP_InputField field)
        {
            if (field.targetGraphic is Image image)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
            }

            var textArea = field.transform.Find("Text Area");
            if (textArea != null)
            {
                EnsureStretch(textArea.gameObject);
                var textAreaRt = textArea.GetComponent<RectTransform>();
                textAreaRt!.offsetMin = new Vector2(32f, 24f);
                textAreaRt.offsetMax = new Vector2(-32f, -24f);
            }

            if (field.placeholder is TextMeshProUGUI placeholder)
            {
                // input.png 已经包含参考图中的占位文案；清掉 TMP 占位文本，避免出现两层文案。
                placeholder.gameObject.SetActive(false);
                field.placeholder = null;
            }

            if (field.textComponent is TextMeshProUGUI text)
            {
                text.fontSize = 30f;
                text.color = new Color(0.30f, 0.19f, 0.12f, 1f);
                text.alignment = TextAlignmentOptions.TopLeft;
                text.raycastTarget = false;
                EnsureStretch(text.gameObject);
            }
        }

        private static void SetupEmotionInputContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<EmotionInputPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;

            var titleGo = EnsureChildText(contentT, uiLayer, "Title_Input", "情绪花园 — 每日心情输入", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(600, 46));
            var ownerGo = EnsureChildText(contentT, uiLayer, "OwnerText", "培育者: —", 20,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(300, 36));

            var inputField = EnsureInputField(contentT, uiLayer);
            var submitBtn = EnsureButtonWithLabel(contentT, uiLayer, "SubmitBtn", "提交心情",
                new Vector2(0, -50), new Vector2(200, 48), new Color(0.25f, 0.45f, 0.7f, 1f));
            var statusGo = EnsureChildText(contentT, uiLayer, "StatusText", "", 16,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(500, 30));
            var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
            statusTmp.color = new Color(1f, 0.85f, 0.4f, 1f);

            // 调试按钮已统一迁移到 Canvas/DevTools 节点
            var oldResetBtn = contentT.Find("DebugResetBtn");
            if (oldResetBtn != null) Object.DestroyImmediate(oldResetBtn.gameObject);
            var oldNextDayBtn = contentT.Find("DebugNextDayBtn");
            if (oldNextDayBtn != null) Object.DestroyImmediate(oldNextDayBtn.gameObject);

            so.FindProperty("_inputField").objectReferenceValue = inputField;
            so.FindProperty("_submitButton").objectReferenceValue = submitBtn;
            so.FindProperty("_statusText").objectReferenceValue = statusTmp;
            so.FindProperty("_ownerText").objectReferenceValue = ownerGo.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        // ── 每周培育面板内容 ──────────────────────────────────

        private const string GardenWeekArtDir = "Assets/_Project/Art/WorldMap/UI/garden_week";

        private static Sprite? LoadGardenWeekSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{GardenWeekArtDir}/{fileName}.png");
        }

        private static Sprite? LoadFlowerSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{FlowerArtDir}/{fileName}.PNG");
        }

        private static void SetupWeeklyGardenContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<WeeklyGardenPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var panelRt = panel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            var contentT = content.transform;
            var uiSprite = LoadGardenWeekSprite("UI");
            var closeSprite = LoadGardenWeekSprite("close");
            var barSprite = LoadGardenWeekSprite("UIbar");
            var bottleSprite = LoadGardenWeekSprite("bottle");
            var soilSprite = LoadFlowerSprite("土壤");
            var flowerHeadIconBindings = BuildFlowerHeadIconBindings();
            var flowerArtCatalog = EnsureFlowerArtCatalog();
            var outlineMaterial = EnsureWeeklyBottleOutlineMaterial();

            // ── 替换 Content 背景为 UI.png ──
            var bgImg = content.GetComponent<Image>();
            if (bgImg != null && uiSprite != null)
            {
                bgImg.sprite = uiSprite;
                bgImg.color = Color.white;
            }

            // ── 标题 + 动态周范围文字 ──
            var existingTitleTmp = contentT.Find("WeekTitle");
            if (existingTitleTmp != null) Object.DestroyImmediate(existingTitleTmp.gameObject);
            var existingWeekInfo = contentT.Find("WeekInfo");
            if (existingWeekInfo != null) Object.DestroyImmediate(existingWeekInfo.gameObject);

            var weekInfoGo = new GameObject("WeekInfo");
            weekInfoGo.transform.SetParent(contentT, false);
            weekInfoGo.layer = uiLayer;
            var wrt = weekInfoGo.AddComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.5f, 1f);
            wrt.anchorMax = new Vector2(0.5f, 1f);
            wrt.pivot = new Vector2(0.5f, 0f);
            wrt.anchoredPosition = new Vector2(0, -90);
            wrt.sizeDelta = new Vector2(500, 36);
            var wtmp = weekInfoGo.AddComponent<TextMeshProUGUI>();
            wtmp.fontSize = 22;
            wtmp.alignment = TextAlignmentOptions.Center;
            wtmp.color = Color.white;
            so.FindProperty("_weekTitleText").objectReferenceValue = wtmp;

            // ── 替换关闭按钮为 close.png 精灵 ──
            var closeBtnT = contentT.Find("Btn_Close");
            if (closeBtnT != null)
            {
                var cbImg = closeBtnT.GetComponent<Image>();
                if (cbImg != null && closeSprite != null)
                {
                    cbImg.sprite = closeSprite;
                    cbImg.color = Color.white;
                    var cbrt = closeBtnT.GetComponent<RectTransform>();
                    cbrt.sizeDelta = new Vector2(50, 52);
                }
                var xT = closeBtnT.Find("X");
                if (xT != null) Object.DestroyImmediate(xT.gameObject);
            }

            // ── 集中信息栏：一个 UIbar 由 Scene 直接作者化和调整 ──
            EnsureWeeklyDetailBar(contentT, uiLayer, barSprite, flowerHeadIconBindings, so);

            // ── 翻周按钮 ──
            var prevBtn = EnsureWeekNavButton(contentT, uiLayer, "PrevWeekBtn", "◀ 上一周", new Vector2(-260, -50));
            var nextBtn = EnsureWeekNavButton(contentT, uiLayer, "NextWeekBtn", "下一周 ▶", new Vector2(260, -50));
            if (prevBtn.onClick.GetPersistentEventCount() == 0)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(prevBtn.onClick, stub.ShowPrevWeek);
            if (nextBtn.onClick.GetPersistentEventCount() == 0)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, stub.ShowNextWeek);

            // ── 瓶子网格 ──
            var gridGo = EnsureChild(contentT, "Grid", uiLayer);
            // 总是重建网格布局（适配新瓶子尺寸）
            var grt = GetOrAdd<RectTransform>(gridGo);
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0, -180);
            grt.sizeDelta = new Vector2(1600, 400);
            // ── 创建瓶子单元格模板 ──
            SetupBottleCellTemplate(gridGo, uiLayer, bottleSprite, soilSprite, flowerHeadIconBindings, outlineMaterial, so);
            EnsureWeeklyBlankClickArea(contentT, uiLayer, stub);

            // ── 数据绑定 ──
            so.FindProperty("_uiBarSprite").objectReferenceValue = barSprite;
            so.FindProperty("_flowerArtCatalog").objectReferenceValue = flowerArtCatalog;
            so.FindProperty("_gridRoot").objectReferenceValue = gridGo.transform;
            so.FindProperty("_nextWeekButton").objectReferenceValue = nextBtn;
            so.ApplyModifiedProperties();
        }

        private static void SetupBottleCellTemplate(GameObject gridGo, int uiLayer,
            Sprite? bottleSprite, Sprite? soilSprite,
            IReadOnlyList<KeyValuePair<string, Sprite>> flowerHeadIconBindings,
            Material? outlineMaterial,
            SerializedObject stubSo)
        {
            // 强制重建 CellTemplate（Bottle + 花卉/土壤 + 天数标签）。集中 UIbar 不属于单日格子。
            var template = gridGo.transform.Find("CellTemplate")?.gameObject;
            if (template != null) Object.DestroyImmediate(template);

            template = new GameObject("CellTemplate");
            template.transform.SetParent(gridGo.transform, false);
            template.SetActive(false);
            template.layer = uiLayer;

            var trt = template.AddComponent<RectTransform>();
            trt.sizeDelta = new Vector2(250, 440);

            // 瓶子背景
            var bottleGo = new GameObject("Bottle");
            bottleGo.transform.SetParent(template.transform, false);
            bottleGo.layer = uiLayer;
            var brt = bottleGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, 48f);
            brt.sizeDelta = new Vector2(170f, 320f);
            var bimg = bottleGo.AddComponent<Image>();
            bimg.preserveAspect = true;
            if (bottleSprite != null)
                bimg.sprite = bottleSprite;
            bimg.color = Color.white;
            ConfigureWeeklyBottleView(bottleGo, bimg, uiLayer);

            var templateSoilImage = EnsureWeeklySoilImage(template.transform, uiLayer, soilSprite);
            var templateFlowerImage = EnsureWeeklyFlowerImage(template.transform, uiLayer);
            templateSoilImage.transform.SetSiblingIndex(templateFlowerImage.transform.GetSiblingIndex());

            // 天数标签
            var dayLabelGo = new GameObject("DayLabel");
            dayLabelGo.transform.SetParent(template.transform, false);
            dayLabelGo.layer = uiLayer;
            var drt = dayLabelGo.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 1f);
            drt.anchorMax = new Vector2(0.5f, 1f);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0, -10);
            drt.sizeDelta = new Vector2(80, 32);

            var daySpriteGo = new GameObject("DaySprite");
            daySpriteGo.transform.SetParent(dayLabelGo.transform, false);
            daySpriteGo.layer = uiLayer;
            var dsrt = daySpriteGo.AddComponent<RectTransform>();
            dsrt.anchorMin = Vector2.zero; dsrt.anchorMax = Vector2.one;
            dsrt.offsetMin = Vector2.zero; dsrt.offsetMax = Vector2.zero;
            var dimg = daySpriteGo.AddComponent<Image>();
            dimg.preserveAspect = true;
            dimg.color = Color.white;

            var dayTextGo = new GameObject("DayText");
            dayTextGo.transform.SetParent(dayLabelGo.transform, false);
            dayTextGo.layer = uiLayer;
            var dtrt = dayTextGo.AddComponent<RectTransform>();
            dtrt.anchorMin = Vector2.zero; dtrt.anchorMax = Vector2.one;
            dtrt.offsetMin = Vector2.zero; dtrt.offsetMax = Vector2.zero;
            var dtmp = dayTextGo.AddComponent<TextMeshProUGUI>();
            dtmp.fontSize = 16;
            dtmp.alignment = TextAlignmentOptions.Center;
            dtmp.color = Color.white;
            dtmp.raycastTarget = false;
            dtmp.enabled = false;
            dayTextGo.SetActive(false);

            // ── 加载 Mon-Sun 精灵，填充 _dayLabelSprites ──
            var daySpritesProp = stubSo.FindProperty("_dayLabelSprites");
            Sprite[] loadedDaySprites = new Sprite[7];
            if (daySpritesProp != null)
            {
                daySpritesProp.arraySize = 7;
                string[] dayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                var daySpriteAssets = AssetDatabase.LoadAllAssetsAtPath(
                    $"{GardenWeekArtDir}/weekUI.psd");
                for (int i = 0; i < 7; i++)
                {
                    Sprite? match = null;
                    foreach (var asset in daySpriteAssets)
                    {
                        if (asset is Sprite s && string.Equals(s.name, dayNames[i], System.StringComparison.OrdinalIgnoreCase))
                        {
                            match = s;
                            break;
                        }
                    }
                    loadedDaySprites[i] = match!;
                    daySpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = match;
                }
            }
            ConfigureDayLabelVariants(daySpriteGo, dimg, uiLayer, loadedDaySprites);

            // 绑定模板到 Stub 的 _cellPrefab
            stubSo.FindProperty("_cellPrefab").objectReferenceValue = template;

            var flowerHeadSpritesProp = stubSo.FindProperty("_flowerHeadIconSprites");
            if (flowerHeadSpritesProp != null)
            {
                flowerHeadSpritesProp.arraySize = flowerHeadIconBindings.Count;
                for (int i = 0; i < flowerHeadIconBindings.Count; i++)
                {
                    flowerHeadSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = flowerHeadIconBindings[i].Value;
                }
            }

            // ── 预置 7 个可见格子到场景中（方便 Scene 视图编辑）──
            string[] dayLabels = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
            for (int i = 0; i < 7; i++)
            {
                var existingDay = gridGo.transform.Find($"Day{i}");
                if (existingDay != null)
                {
                    EnsureWeeklyGardenCell(existingDay.gameObject, i, stubSo, uiLayer, bottleSprite, soilSprite, outlineMaterial, flowerHeadIconBindings);
                    continue;
                }

                var dayCell = Object.Instantiate(template, gridGo.transform);
                dayCell.name = $"Day{i}";
                dayCell.SetActive(true);
                dayCell.layer = uiLayer;

                // 天数文字
                var dayTmp = dayCell.transform.Find("DayLabel/DayText")?.GetComponent<TextMeshProUGUI>();
                if (dayTmp != null)
                {
                    dayTmp.text = dayLabels[i];
                    dayTmp.enabled = false;
                    dayTmp.gameObject.SetActive(false);
                }
                var dayCellSprite = dayCell.transform.Find("DayLabel/DaySprite");
                if (dayCellSprite != null) dayCellSprite.gameObject.SetActive(true);
                var dayCellText = dayCell.transform.Find("DayLabel/DayText")?.GetComponent<TextMeshProUGUI>();
                if (dayCellText != null) dayCellText.enabled = false;

                // 示例文字
                var label = dayCell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = dayLabels[i];
                }

                EnsureWeeklyGardenCell(dayCell, i, stubSo, uiLayer, bottleSprite, soilSprite, outlineMaterial, flowerHeadIconBindings);
            }
        }

        private static void EnsureWeeklyGardenCell(GameObject cell, int dayIndex, SerializedObject stubSo, int uiLayer,
            Sprite? bottleSprite, Sprite? soilSprite, Material? outlineMaterial,
            IReadOnlyList<KeyValuePair<string, Sprite>> flowerHeadIconBindings)
        {
            var cellRt = GetOrAdd<RectTransform>(cell);
            cellRt.sizeDelta = new Vector2(250f, 440f);

            var bottle = EnsureChild(cell.transform, "Bottle", uiLayer);
            var bottleRt = GetOrAdd<RectTransform>(bottle);
            bottleRt.anchorMin = new Vector2(0.5f, 0.5f);
            bottleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bottleRt.pivot = new Vector2(0.5f, 0.5f);
            bottleRt.anchoredPosition = new Vector2(0f, 48f);
            bottleRt.sizeDelta = new Vector2(170f, 320f);
            var bottleImg = GetOrAdd<Image>(bottle);
            bottleImg.preserveAspect = true;
            if (bottleSprite != null) bottleImg.sprite = bottleSprite;
            bottleImg.color = Color.white;
            ConfigureWeeklyBottleView(bottle, bottleImg, uiLayer);
            EnsureWeeklyBottleInteraction(bottle, dayIndex, stubSo, uiLayer, bottleSprite, outlineMaterial);

            var cellSoilImage = EnsureWeeklySoilImage(cell.transform, uiLayer, soilSprite);
            var cellFlowerImage = EnsureWeeklyFlowerImage(cell.transform, uiLayer);
            cellSoilImage.transform.SetSiblingIndex(cellFlowerImage.transform.GetSiblingIndex());

            var legacyGrowth = cell.transform.Find("Growth");
            if (legacyGrowth != null)
            {
                Object.DestroyImmediate(legacyGrowth.gameObject);
            }

            var legacyBar = cell.transform.Find("UIbar");
            if (legacyBar != null)
            {
                Object.DestroyImmediate(legacyBar.gameObject);
            }

            var daySprite = cell.transform.Find("DayLabel/DaySprite");
            var dayImage = daySprite?.GetComponent<Image>();
            if (daySprite != null && dayImage != null)
            {
                ConfigureDayLabelVariants(daySprite.gameObject, dayImage, uiLayer, LoadDayLabelSprites());
            }

            var dayText = cell.transform.Find("DayLabel/DayText");
            if (dayText != null)
            {
                var dayTextComponent = dayText.GetComponent<TextMeshProUGUI>();
                if (dayTextComponent != null) dayTextComponent.enabled = false;
                dayText.gameObject.SetActive(false);
            }

            var legacyLabel = cell.transform.Find("Label");
            if (legacyLabel != null) legacyLabel.gameObject.SetActive(false);

        }

        private static Material? EnsureWeeklyBottleOutlineMaterial()
        {
            var shader = Shader.Find(WeeklyOutlineShaderName);
            if (shader == null)
            {
                Debug.LogError($"[WorldMapEmotionGardenUI] 未找到瓶子边缘高亮 Shader: {WeeklyOutlineShaderName}");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(WeeklyOutlineMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "SelectedBottleOutline"
                };
                AssetDatabase.CreateAsset(material, WeeklyOutlineMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_OutlineColor", new Color(1f, 0.82f, 0.22f, 0.95f));
            material.SetFloat("_OutlineThickness", 1.5f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureWeeklyDetailBar(
            Transform content,
            int uiLayer,
            Sprite? barSprite,
            IReadOnlyList<KeyValuePair<string, Sprite>> flowerHeadIconBindings,
            SerializedObject stubSo)
        {
            var bar = EnsureChild(content, "UIbar", uiLayer);
            var barRt = GetOrAdd<RectTransform>(bar);
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.localScale = new Vector3(1.5f, 1.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, 92f);
            barRt.sizeDelta = new Vector2(240f, 68f);

            var barImg = GetOrAdd<Image>(bar);
            barImg.sprite = barSprite;
            barImg.preserveAspect = true;
            barImg.color = Color.white;
            barImg.raycastTarget = false;

            var growth = EnsureChild(bar.transform, "Growth", uiLayer);
            var growthRt = GetOrAdd<RectTransform>(growth);
            growthRt.anchorMin = new Vector2(0.5f, 0.5f);
            growthRt.anchorMax = new Vector2(0.5f, 0.5f);
            growthRt.pivot = new Vector2(0.5f, 0.5f);
            growthRt.sizeDelta = new Vector2(36f, 36f);
            growthRt.anchoredPosition = new Vector2(-96f, 18f);
            var growthImg = GetOrAdd<Image>(growth);
            growthImg.sprite = flowerHeadIconBindings.Count > 0 ? flowerHeadIconBindings[0].Value : null;
            growthImg.preserveAspect = true;
            growthImg.color = Color.white;
            growthImg.raycastTarget = false;
            ConfigureFlowerHeadIconVariants(growth, growthImg, uiLayer, flowerHeadIconBindings);

            var dateText = CreateWeeklyInfoText(bar.transform, uiLayer, "DateText", "---",
                new Vector2(-78f, -4f), new Vector2(56f, 50f), 18f);
            var emotionText = CreateWeeklyInfoText(bar.transform, uiLayer, "EmotionText", "---",
                new Vector2(0f, 0f), new Vector2(82f, 54f), 16f);
            var flowerLanguageText = CreateWeeklyInfoText(bar.transform, uiLayer, "FlowerLanguageText", "---",
                new Vector2(78f, 0f), new Vector2(82f, 64f), 18f);

            stubSo.FindProperty("_detailBarRoot").objectReferenceValue = bar.transform;
            stubSo.FindProperty("_detailGrowthView").objectReferenceValue = growth.GetComponent<SceneAuthoredImageVariantView>();
            stubSo.FindProperty("_detailDateText").objectReferenceValue = dateText;
            stubSo.FindProperty("_detailEmotionText").objectReferenceValue = emotionText;
            stubSo.FindProperty("_detailFlowerLanguageText").objectReferenceValue = flowerLanguageText;
        }

        private static void EnsureWeeklyBlankClickArea(Transform content, int uiLayer, WeeklyGardenPanelStub stub)
        {
            var blank = EnsureChild(content, "BlankClickArea", uiLayer);
            var rt = GetOrAdd<RectTransform>(blank);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(0);

            var image = GetOrAdd<Image>(blank);
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            var button = GetOrAdd<Button>(blank);
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, stub.ClearDaySelection);
            }
        }

        private static void EnsureWeeklyBottleInteraction(
            GameObject bottle,
            int dayIndex,
            SerializedObject stubSo,
            int uiLayer,
            Sprite? bottleSprite,
            Material? outlineMaterial)
        {
            var highlight = EnsureChild(bottle.transform, "SelectedHighlight", uiLayer);
            var highlightRt = GetOrAdd<RectTransform>(highlight);
            highlightRt.anchorMin = Vector2.zero;
            highlightRt.anchorMax = Vector2.one;
            highlightRt.offsetMin = Vector2.zero;
            highlightRt.offsetMax = Vector2.zero;
            highlightRt.SetAsLastSibling();

            var highlightImage = GetOrAdd<Image>(highlight);
            highlightImage.sprite = bottleSprite;
            highlightImage.preserveAspect = true;
            // Alpha 边缘材质只输出轮廓；Image 本身保持不透明白色，避免整张 Sprite 被 Outline 填充。
            highlightImage.color = Color.white;
            highlightImage.material = outlineMaterial;
            highlightImage.enabled = outlineMaterial != null;
            highlightImage.raycastTarget = false;
            var legacyOutline = highlight.GetComponent<Outline>();
            if (legacyOutline != null)
            {
                Object.DestroyImmediate(legacyOutline);
            }
            highlight.SetActive(false);

            var interaction = GetOrAdd<WeeklyGardenBottleInteraction>(bottle);
            var interactionSo = new SerializedObject(interaction);
            interactionSo.FindProperty("_panel").objectReferenceValue = stubSo.targetObject;
            interactionSo.FindProperty("_dayIndex").intValue = dayIndex;
            interactionSo.FindProperty("_scaleTarget").objectReferenceValue = bottle.GetComponent<RectTransform>();
            interactionSo.FindProperty("_selectedHighlight").objectReferenceValue = highlight;
            interactionSo.FindProperty("_normalScale").vector3Value = Vector3.one;
            interactionSo.FindProperty("_hoverScale").vector3Value = new Vector3(1.06f, 1.06f, 1f);
            interactionSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image EnsureWeeklyFlowerImage(Transform parent, int uiLayer)
        {
            var go = EnsureChild(parent, "FlowerImage", uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 28f);
            rt.sizeDelta = new Vector2(132f, 156f);

            var image = GetOrAdd<Image>(go);
            var previewBinding = BuildBloomedFlowerBindings();
            if (previewBinding.Count > 0)
            {
                image.sprite = previewBinding[0].Value;
            }
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.enabled = false;
            string previewKey = previewBinding.Count > 0 ? previewBinding[0].Key : string.Empty;
            ConfigureImageVariantView(go, image, uiLayer, previewBinding, previewKey);
            return image;
        }

        private static Image EnsureWeeklySoilImage(Transform parent, int uiLayer, Sprite? soilSprite)
        {
            var go = EnsureChild(parent, "SoilImage", uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(132f, 72f);

            var image = GetOrAdd<Image>(go);
            image.sprite = soilSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.enabled = false;
            go.SetActive(false);
            return image;
        }

        private static List<KeyValuePair<string, Sprite>> BuildBloomedFlowerBindings()
        {
            var bindings = new List<KeyValuePair<string, Sprite>>();
            foreach (string emotionType in FlowerArtEmotionTypes)
            {
                foreach (string owner in new[] { EmotionFlowerCatalog.OwnerAngel, EmotionFlowerCatalog.OwnerDemon })
                {
                    string ownerDisplayName = owner == EmotionFlowerCatalog.OwnerDemon ? "恶魔" : "天使";
                    Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"{FlowerArtDir}/{ownerDisplayName}-{emotionType}（完整）.PNG");
                    if (sprite == null)
                    {
                        continue;
                    }

                    string key = SceneAuthoredImageVariantView.BuildFlowerKey(
                        emotionType,
                        owner,
                        GrowthState.Bloomed);
                    bindings.Add(new KeyValuePair<string, Sprite>(key, sprite));
                }
            }

            return bindings;
        }

        private static List<KeyValuePair<string, Sprite>> BuildFlowerHeadIconBindings()
        {
            var bindings = new List<KeyValuePair<string, Sprite>>();
            foreach (string emotionType in FlowerArtEmotionTypes)
            {
                foreach (string owner in new[] { EmotionFlowerCatalog.OwnerAngel, EmotionFlowerCatalog.OwnerDemon })
                {
                    string ownerDisplayName = owner == EmotionFlowerCatalog.OwnerDemon ? "恶魔" : "天使";
                    Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"{FlowerHeadArtDir}/{ownerDisplayName}-{emotionType}.PNG");
                    if (sprite == null)
                    {
                        Debug.LogWarning($"[WorldMapEmotionGardenUI] 缺少每周信息条花头资源: {ownerDisplayName}-{emotionType}.PNG");
                        continue;
                    }

                    string key = SceneAuthoredImageVariantView.BuildKey(
                        "flower-head",
                        EmotionFlowerCatalog.NormalizeOwner(owner),
                        EmotionFlowerCatalog.NormalizeEmotionType(emotionType));
                    bindings.Add(new KeyValuePair<string, Sprite>(key, sprite));
                }
            }

            return bindings;
        }

        private static void ConfigureImageVariantView(GameObject owner, Image preview,
            int uiLayer, IReadOnlyList<KeyValuePair<string, Sprite>> bindings, string previewKey,
            bool showPreviewInScene = false)
        {
            for (int i = owner.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = owner.transform.GetChild(i);
                if (child.name.StartsWith("Variant_", System.StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var variantKeys = new List<string>();
            var variantTargets = new List<GameObject>();
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (string.Equals(binding.Key, previewKey, System.StringComparison.Ordinal))
                {
                    preview.sprite = binding.Value;
                    continue;
                }

                var variant = new GameObject($"Variant_{i:00}");
                variant.transform.SetParent(owner.transform, false);
                variant.layer = uiLayer;
                var rt = variant.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                var image = variant.AddComponent<Image>();
                image.sprite = binding.Value;
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
                variant.SetActive(false);
                variantKeys.Add(binding.Key);
                variantTargets.Add(variant);
            }

            var existingViews = owner.GetComponents<SceneAuthoredImageVariantView>();
            for (int i = 0; i < existingViews.Length; i++)
            {
                Object.DestroyImmediate(existingViews[i]);
            }

            var view = owner.AddComponent<SceneAuthoredImageVariantView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_previewImage").objectReferenceValue = preview;
            var previewTarget = viewSo.FindProperty("_previewTarget");
            if (previewTarget != null) previewTarget.objectReferenceValue = null;
            viewSo.FindProperty("_previewKey").stringValue = previewKey;
            var variantsProp = viewSo.FindProperty("_variants");
            variantsProp.arraySize = variantTargets.Count;
            for (int i = 0; i < variantTargets.Count; i++)
            {
                var element = variantsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Key").stringValue = variantKeys[i];
                element.FindPropertyRelative("Target").objectReferenceValue = variantTargets[i];
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            preview.enabled = showPreviewInScene;
            owner.SetActive(showPreviewInScene);
        }

        private static SceneAuthoredImageVariantView EnsureDetailFlowerVariantView(
            Transform detailView,
            int uiLayer,
            Sprite? soilSprite,
            IReadOnlyList<KeyValuePair<string, Sprite>> bindings)
        {
            var owner = EnsureChild(detailView, "FlowerImage", uiLayer);
            ApplyRect(owner, new Vector2(-392f, 20f), new Vector2(330f, 330f));

            var oldImage = owner.GetComponent<Image>();
            if (oldImage != null)
            {
                Object.DestroyImmediate(oldImage);
            }

            for (int i = owner.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = owner.transform.GetChild(i);
                if (child.name.StartsWith("Variant_", System.StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            string previewKey = bindings.Count > 0 ? bindings[0].Key : string.Empty;
            GameObject? previewTarget = null;
            var variantKeys = new List<string>();
            var variantTargets = new List<GameObject>();

            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                var pair = new GameObject($"Variant_{i:00}");
                pair.transform.SetParent(owner.transform, false);
                pair.layer = uiLayer;
                var pairRt = pair.AddComponent<RectTransform>();
                pairRt.anchorMin = Vector2.zero;
                pairRt.anchorMax = Vector2.one;
                pairRt.offsetMin = Vector2.zero;
                pairRt.offsetMax = Vector2.zero;
                pairRt.pivot = new Vector2(0.5f, 0.5f);

                var flower = EnsureImageChild(pair.transform, "FlowerArt", uiLayer, binding.Value,
                    Vector2.zero, new Vector2(330f, 330f));
                ApplyRect(flower.gameObject, Vector2.zero, new Vector2(330f, 330f));
                flower.color = Color.white;
                flower.raycastTarget = false;

                float soilY = ResolveDetailSoilY(binding.Key);
                var soil = EnsureImageChild(pair.transform, "SoilImage", uiLayer, soilSprite,
                    new Vector2(0f, soilY), new Vector2(330f, 100f));
                ApplyRect(soil.gameObject, new Vector2(0f, soilY), new Vector2(330f, 100f));
                soil.color = Color.white;
                soil.raycastTarget = false;

                pair.SetActive(false);
                if (string.Equals(binding.Key, previewKey, System.StringComparison.Ordinal))
                {
                    previewTarget = pair;
                }
                else
                {
                    variantKeys.Add(binding.Key);
                    variantTargets.Add(pair);
                }
            }

            var existingViews = owner.GetComponents<SceneAuthoredImageVariantView>();
            for (int i = 0; i < existingViews.Length; i++)
            {
                Object.DestroyImmediate(existingViews[i]);
            }

            var view = owner.AddComponent<SceneAuthoredImageVariantView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_previewImage").objectReferenceValue = null;
            viewSo.FindProperty("_previewTarget").objectReferenceValue = previewTarget;
            viewSo.FindProperty("_previewKey").stringValue = previewKey;
            var variantsProp = viewSo.FindProperty("_variants");
            variantsProp.arraySize = variantTargets.Count;
            for (int i = 0; i < variantTargets.Count; i++)
            {
                var element = variantsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Key").stringValue = variantKeys[i];
                element.FindPropertyRelative("Target").objectReferenceValue = variantTargets[i];
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(false);
            return view;
        }

        private static float ResolveDetailSoilY(string flowerKey)
        {
            string[] parts = flowerKey.Split('|');
            if (parts.Length >= 2 && DetailSoilYByFlower.TryGetValue($"{parts[1]}|{parts[0]}", out float y))
            {
                return y;
            }

            return -164f;
        }

        private static void ConfigureWeeklyBottleView(GameObject bottle, Image preview, int uiLayer)
        {
            // PSD 中的 bottle/复制层只是七天排版副本，不是状态变体。
            // 所有日期始终显示同一个作者化 bottle.png。
            ConfigureImageVariantView(
                bottle,
                preview,
                uiLayer,
                new List<KeyValuePair<string, Sprite>>(),
                SceneAuthoredImageVariantView.BuildKey("bottle", string.Empty, "default"),
                true);
        }

        private static void ConfigureFlowerHeadIconVariants(GameObject growth, Image preview, int uiLayer,
            IReadOnlyList<KeyValuePair<string, Sprite>> bindings)
        {
            string previewKey = bindings.Count > 0 ? bindings[0].Key : string.Empty;
            ConfigureImageVariantView(growth, preview, uiLayer, bindings, previewKey);
        }

        private static void ConfigureDayLabelVariants(GameObject daySprite, Image preview, int uiLayer,
            IReadOnlyList<Sprite?> daySprites)
        {
            var bindings = new List<KeyValuePair<string, Sprite>>();
            for (int i = 0; i < daySprites.Count; i++)
            {
                Sprite? sprite = daySprites[i];
                if (sprite == null)
                {
                    continue;
                }

                bindings.Add(new KeyValuePair<string, Sprite>(
                    SceneAuthoredImageVariantView.BuildKey("day", string.Empty, i.ToString()), sprite));
            }

            string previewKey = bindings.Count > 0 ? bindings[0].Key : string.Empty;
            ConfigureImageVariantView(daySprite, preview, uiLayer, bindings, previewKey, true);
        }

        private static List<Sprite?> LoadDayLabelSprites()
        {
            var result = new List<Sprite?>();
            string[] names = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var assets = AssetDatabase.LoadAllAssetsAtPath($"{GardenWeekArtDir}/weekUI.psd");
            foreach (string name in names)
            {
                Sprite? match = null;
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite && string.Equals(sprite.name, name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = sprite;
                        break;
                    }
                }
                result.Add(match);
            }

            return result;
        }

        private static TextMeshProUGUI CreateWeeklyInfoText(Transform parent, int uiLayer, string name,
            string text, Vector2 position, Vector2 size, float fontSize)
        {
            var go = EnsureChild(parent, name, uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.35f, 0.2f, 0.08f, 1f);
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        /// <summary>顶部锚定的翻周导航按钮（EnsureButtonWithLabel 是中心锚定，不适用标题栏）。</summary>
        private static Button EnsureWeekNavButton(Transform parent, int uiLayer, string name, string label, Vector2 anchoredPosition)
        {
            var go = EnsureChild(parent, name, uiLayer);
            bool isNew = go.transform.childCount == 0;
            var btn = GetOrAdd<Button>(go);

            if (isNew)
            {
                var rt = GetOrAdd<RectTransform>(go);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = new Vector2(120, 40);
                var img = GetOrAdd<Image>(go);
                img.color = new Color(0.22f, 0.3f, 0.42f, 1f);

                var labelGo = EnsureChild(go.transform, "Label", uiLayer);
                var lrt = GetOrAdd<RectTransform>(labelGo);
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var tmp = GetOrAdd<TextMeshProUGUI>(labelGo);
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 16;
                tmp.color = Color.white;
            }

            return btn;
        }

        // ── 情绪图鉴面板内容 ──────────────────────────────────

        private static void SetupFlowerCollectionBookContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<FlowerCollectionPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;
            var contentImage = GetOrAdd<Image>(content);
            contentImage.color = new Color(1f, 1f, 1f, 0f);
            contentImage.raycastTarget = true;

            foreach (var oldName in new[] { "CollectionTitle", "ScrollView", "DebugBloomBtn" })
            {
                var old = contentT.Find(oldName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var oldClose = contentT.Find("Btn_Close");
            if (oldClose != null) oldClose.gameObject.SetActive(false);
            var topResource = contentT.Find("TopResource");
            if (topResource != null) topResource.gameObject.SetActive(false);

            var codexBookSprite = LoadFlowerCodexSprite("book");
            var cardSprite = LoadFlowerCodexSprite("card");
            var unknownSprite = LoadFlowerCodexSprite("unknow");
            var codexCloseSprite = LoadFlowerCodexSprite("close");
            var codexLeftSprite = LoadFlowerCodexSprite("left");
            var codexRightSprite = LoadFlowerCodexSprite("right");
            var detailBookSprite = LoadFlowerInfoSprite("book");
            var detailStockSprite = LoadFlowerInfoSprite("stock");
            var detailCloseSprite = LoadFlowerInfoSprite("close");
            var detailLeftSprite = LoadFlowerInfoSprite("left");
            var detailRightSprite = LoadFlowerInfoSprite("right");
            var soilSprite = LoadFlowerSprite("土壤");
            var flowerArtCatalog = EnsureFlowerArtCatalog();

            var codexView = EnsureFullRect(contentT, "CodexView", uiLayer);
            var detailView = EnsureFullRect(contentT, "DetailView", uiLayer);

            var codexBook = EnsureImageChild(codexView.transform, "Book", uiLayer, codexBookSprite, Vector2.zero, new Vector2(1644, 951));
            ApplyRect(codexBook.gameObject, Vector2.zero, new Vector2(1644, 951));
            codexBook.transform.SetAsFirstSibling();

            EnsureCodexTitlePlate(codexView.transform, uiLayer);
            EnsureCodexCategoryTabs(codexView.transform, uiLayer);

            var codexClose = EnsureImageButton(codexView.transform, "CloseButton", uiLayer, codexCloseSprite,
                new Vector2(728, 372), new Vector2(58, 58));
            ApplyRect(codexClose.gameObject, new Vector2(728, 372), new Vector2(58, 58));
            var codexPrevious = EnsureImageButton(codexView.transform, "PreviousPageButton", uiLayer, codexLeftSprite,
                new Vector2(-752, -12), new Vector2(92, 120));
            ApplyRect(codexPrevious.gameObject, new Vector2(-752, -12), new Vector2(92, 120));
            var codexNext = EnsureImageButton(codexView.transform, "NextPageButton", uiLayer, codexRightSprite,
                new Vector2(752, -12), new Vector2(92, 120));
            ApplyRect(codexNext.gameObject, new Vector2(752, -12), new Vector2(92, 120));

            var progressText = EnsureTextChild(codexView.transform, uiLayer, "ProgressText", "收集进度：0 / 12", 24,
                new Vector2(-118, 302), new Vector2(420, 42), new Color(0.42f, 0.25f, 0.13f, 1f), TextAlignmentOptions.Center);
            progressText.fontSize = 24;
            var pageText = EnsureTextChild(codexView.transform, uiLayer, "PageText", "1 / 1", 22,
                new Vector2(-250, -348), new Vector2(190, 38), new Color(0.42f, 0.25f, 0.13f, 1f), TextAlignmentOptions.Center);
            var clickHint = EnsureTextChild(codexView.transform, uiLayer, "ClickHintText", "点击花卉可查看详情", 22,
                new Vector2(290, -348), new Vector2(380, 38), new Color(0.42f, 0.25f, 0.13f, 1f), TextAlignmentOptions.Center);
            clickHint.fontStyle = FontStyles.Normal;

            var cardsRoot = EnsureChild(codexView.transform, "Cards", uiLayer);
            var cardsRootRt = GetOrAdd<RectTransform>(cardsRoot);
            cardsRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRootRt.pivot = new Vector2(0.5f, 0.5f);
            cardsRootRt.anchoredPosition = Vector2.zero;
            cardsRootRt.sizeDelta = Vector2.zero;

            var cardSlotsProp = so.FindProperty("_cardSlots");
            if (cardSlotsProp != null) cardSlotsProp.arraySize = 12;

            var cardPositions = new[]
            {
                new Vector2(-534, 118), new Vector2(-354, 118), new Vector2(-174, 118),
                new Vector2(-534, -142), new Vector2(-354, -142), new Vector2(-174, -142),
                new Vector2(210, 118), new Vector2(390, 118), new Vector2(570, 118),
                new Vector2(210, -142), new Vector2(390, -142), new Vector2(570, -142)
            };

            for (int i = 0; i < cardPositions.Length; i++)
            {
                var slot = EnsureCodexCardSlot(cardsRoot.transform, uiLayer, i, cardPositions[i], cardSprite, unknownSprite, soilSprite);
                ApplyRect(slot, cardPositions[i], new Vector2(154, 216));
                if (cardSlotsProp != null)
                {
                    BindCardSlot(cardSlotsProp.GetArrayElementAtIndex(i), slot);
                }
            }

            EnsureImageChild(detailView.transform, "Book", uiLayer, detailBookSprite, Vector2.zero, new Vector2(1663, 966));
            var detailClose = EnsureImageButton(detailView.transform, "CloseButton", uiLayer, detailCloseSprite,
                new Vector2(728, 372), new Vector2(58, 58));
            var detailBack = EnsureImageButton(detailView.transform, "BackButton", uiLayer, detailLeftSprite,
                new Vector2(-728, 372), new Vector2(58, 58));
            var detailPrevious = EnsureImageButton(detailView.transform, "PreviousButton", uiLayer, detailLeftSprite,
                new Vector2(-768, -8), new Vector2(68, 96));
            var detailNext = EnsureImageButton(detailView.transform, "NextButton", uiLayer, detailRightSprite,
                new Vector2(768, -8), new Vector2(68, 96));

            var oldDetailSoil = detailView.transform.Find("SoilImage");
            if (oldDetailSoil != null)
            {
                Object.DestroyImmediate(oldDetailSoil.gameObject);
            }

            var detailFlowerBindings = BuildBloomedFlowerBindings();
            var detailFlowerView = EnsureDetailFlowerVariantView(
                detailView.transform, uiLayer, soilSprite, detailFlowerBindings);
            var detailNumber = EnsureTextChild(detailView.transform, uiLayer, "NumberText", "No. 027", 28,
                new Vector2(-392, -282), new Vector2(260, 46), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Center);

            var stockPlate = EnsureImageChild(detailView.transform, "StockPlate", uiLayer, detailStockSprite,
                new Vector2(-210, -306), new Vector2(220, 30));
            stockPlate.raycastTarget = false;
            var stockText = EnsureTextChild(stockPlate.transform, uiLayer, "StockText", "0", 18,
                Vector2.zero, new Vector2(200, 28), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Center);

            var detailName = EnsureTextChild(detailView.transform, uiLayer, "NameText", "情绪之花", 40,
                new Vector2(330, 218), new Vector2(460, 64), new Color(0.33f, 0.18f, 0.1f, 1f), TextAlignmentOptions.Center);
            var detailCreated = EnsureTextChild(detailView.transform, uiLayer, "CreatedText", "累计收集 0 朵", 24,
                new Vector2(330, 142), new Vector2(420, 44), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Center);
            var detailEmotion = EnsureTextChild(detailView.transform, uiLayer, "EmotionText", "情绪", 24,
                new Vector2(220, 62), new Vector2(240, 40), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Left);
            var detailOwner = EnsureTextChild(detailView.transform, uiLayer, "OwnerText", "培育者", 24,
                new Vector2(475, 62), new Vector2(240, 40), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Left);
            var phraseTitle = EnsureTextChild(detailView.transform, uiLayer, "PhraseTitleText", "花语", 30,
                new Vector2(330, -70), new Vector2(460, 46), new Color(0.33f, 0.18f, 0.1f, 1f), TextAlignmentOptions.Center);
            var phraseBody = EnsureTextChild(detailView.transform, uiLayer, "PhraseBodyText", "在这里显示这朵花的记录。", 22,
                new Vector2(330, -186), new Vector2(560, 150), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.TopLeft);

            so.FindProperty("_codexView").objectReferenceValue = codexView;
            so.FindProperty("_detailView").objectReferenceValue = detailView;
            so.FindProperty("_flowerArtCatalog").objectReferenceValue = flowerArtCatalog;
            so.FindProperty("_progressText").objectReferenceValue = progressText;
            so.FindProperty("_pageText").objectReferenceValue = pageText;
            so.FindProperty("_previousPageButton").objectReferenceValue = codexPrevious;
            so.FindProperty("_nextPageButton").objectReferenceValue = codexNext;
            so.FindProperty("_detailFlowerView").objectReferenceValue = detailFlowerView;
            so.FindProperty("_detailSoilImage").objectReferenceValue = null;
            so.FindProperty("_detailNumberText").objectReferenceValue = detailNumber;
            so.FindProperty("_detailNameText").objectReferenceValue = detailName;
            so.FindProperty("_detailCreatedText").objectReferenceValue = detailCreated;
            so.FindProperty("_detailEmotionText").objectReferenceValue = detailEmotion;
            so.FindProperty("_detailOwnerText").objectReferenceValue = detailOwner;
            so.FindProperty("_detailPhraseTitleText").objectReferenceValue = phraseTitle;
            so.FindProperty("_detailPhraseBodyText").objectReferenceValue = phraseBody;
            so.FindProperty("_detailStockText").objectReferenceValue = stockText;
            so.FindProperty("_detailBackButton").objectReferenceValue = detailBack;
            so.FindProperty("_detailPreviousButton").objectReferenceValue = detailPrevious;
            so.FindProperty("_detailNextButton").objectReferenceValue = detailNext;
            so.FindProperty("_detailCloseButton").objectReferenceValue = detailClose;
            so.FindProperty("_closeButton").objectReferenceValue = codexClose;
            so.ApplyModifiedProperties();

            codexView.SetActive(true);
            detailView.SetActive(false);
        }

        private static Sprite? LoadFlowerCodexSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{FlowerCodexArtDir}/{fileName}.png");
        }

        private static Sprite? LoadFlowerInfoSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{FlowerInfoArtDir}/{fileName}.png");
        }

        private static EmotionFlowerArtCatalog EnsureFlowerArtCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EmotionFlowerArtCatalog>(FlowerArtCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EmotionFlowerArtCatalog>();
                AssetDatabase.CreateAsset(catalog, FlowerArtCatalogPath);
            }

            var catalogSo = new SerializedObject(catalog);
            var entries = catalogSo.FindProperty("_entries");
            int entryCount = FlowerArtEmotionTypes.Length * 2;
            entries.arraySize = entryCount;

            int entryIndex = 0;
            foreach (string emotionType in FlowerArtEmotionTypes)
            {
                foreach (string owner in new[] { EmotionFlowerCatalog.OwnerAngel, EmotionFlowerCatalog.OwnerDemon })
                {
                    string ownerDisplayName = owner == EmotionFlowerCatalog.OwnerDemon ? "恶魔" : "天使";
                    var entry = entries.GetArrayElementAtIndex(entryIndex++);
                    entry.FindPropertyRelative("EmotionType").stringValue = emotionType;
                    entry.FindPropertyRelative("Owner").stringValue = owner;

                    var growingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"{FlowerArtDir}/{ownerDisplayName}-{emotionType}.PNG");
                    var bloomedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"{FlowerArtDir}/{ownerDisplayName}-{emotionType}（完整）.PNG");

                    entry.FindPropertyRelative("GrowingSprite").objectReferenceValue = growingSprite;
                    entry.FindPropertyRelative("BloomedSprite").objectReferenceValue = bloomedSprite;

                    if (growingSprite == null)
                    {
                        Debug.LogWarning($"[WorldMapEmotionGardenUI] 未找到花卉资源: {ownerDisplayName}-{emotionType}.PNG");
                    }
                    else if (bloomedSprite == null)
                    {
                        Debug.LogWarning($"[WorldMapEmotionGardenUI] 未找到完整花卉资源: {ownerDisplayName}-{emotionType}（完整）.PNG，已保持为空");
                    }
                }
            }

            catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static GameObject EnsureFullRect(Transform parent, string name, int uiLayer)
        {
            var go = EnsureChild(parent, name, uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static Image EnsureImageChild(Transform parent, string name, int uiLayer, Sprite? sprite,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            bool isNew = parent.Find(name) == null;
            var go = EnsureChild(parent, name, uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            if (isNew)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
            }

            var image = GetOrAdd<Image>(go);
            image.sprite = sprite;
            image.color = sprite == null ? new Color(1f, 1f, 1f, 0.16f) : Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Button EnsureImageButton(Transform parent, string name, int uiLayer, Sprite? sprite,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var image = EnsureImageChild(parent, name, uiLayer, sprite, anchoredPosition, sizeDelta);
            image.raycastTarget = true;
            if (sprite == null) image.color = new Color(0.35f, 0.2f, 0.12f, 0.55f);
            var button = GetOrAdd<Button>(image.gameObject);
            button.targetGraphic = image;
            return button;
        }

        private static TextMeshProUGUI EnsureTextChild(Transform parent, int uiLayer, string name, string text, int fontSize,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color, TextAlignmentOptions alignment)
        {
            bool isNew = parent.Find(name) == null;
            var go = EnsureChild(parent, name, uiLayer);
            var rt = GetOrAdd<RectTransform>(go);
            if (isNew)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
            }

            var tmp = GetOrAdd<TextMeshProUGUI>(go);
            if (isNew || string.IsNullOrEmpty(tmp.text)) tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void EnsureCodexTitlePlate(Transform parent, int uiLayer)
        {
            var plateGo = EnsureChild(parent, "TitlePlate", uiLayer);
            ApplyRect(plateGo, new Vector2(-510, 315), new Vector2(260, 64));
            var plateImage = GetOrAdd<Image>(plateGo);
            plateImage.color = new Color(0.92f, 0.76f, 0.46f, 0.76f);
            plateImage.raycastTarget = false;

            var title = EnsureTextChild(plateGo.transform, uiLayer, "TitleText", "花卉图鉴", 32,
                Vector2.zero, new Vector2(230, 48), new Color(0.38f, 0.2f, 0.1f, 1f), TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
        }

        private static void EnsureCodexCategoryTabs(Transform parent, int uiLayer)
        {
            var tabsRoot = EnsureChild(parent, "CategoryTabs", uiLayer);
            ApplyRect(tabsRoot, new Vector2(400, 300), new Vector2(560, 52));

            string[] labels = { "全部", "温柔", "活力", "治愈", "稀有" };
            for (int i = 0; i < labels.Length; i++)
            {
                var tabGo = EnsureChild(tabsRoot.transform, $"CategoryTab_{i:00}", uiLayer);
                ApplyRect(tabGo, new Vector2(-224 + i * 112, 0), new Vector2(96, 40));
                var tabImage = GetOrAdd<Image>(tabGo);
                tabImage.color = i == 0
                    ? new Color(0.92f, 0.68f, 0.32f, 0.9f)
                    : new Color(0.84f, 0.66f, 0.42f, 0.45f);
                tabImage.raycastTarget = false;

                var label = EnsureTextChild(tabGo.transform, uiLayer, "Label", labels[i], 22,
                    Vector2.zero, new Vector2(86, 30), new Color(0.42f, 0.25f, 0.13f, 1f), TextAlignmentOptions.Center);
                label.fontStyle = i == 0 ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private static void ApplyRect(GameObject go, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static GameObject EnsureCodexCardSlot(Transform parent, int uiLayer, int index, Vector2 anchoredPosition,
            Sprite? cardSprite, Sprite? unknownSprite, Sprite? soilSprite)
        {
            string name = $"CodexCardSlot_{index:00}";
            var slot = EnsureChild(parent, name, uiLayer);
            var rt = GetOrAdd<RectTransform>(slot);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(154, 216);

            var cardImage = GetOrAdd<Image>(slot);
            cardImage.sprite = cardSprite;
            cardImage.color = cardSprite == null ? new Color(1f, 1f, 1f, 0.16f) : Color.white;
            cardImage.preserveAspect = true;
            var button = GetOrAdd<Button>(slot);
            button.targetGraphic = cardImage;

            var lockedImage = EnsureImageChild(slot.transform, "LockedImage", uiLayer, unknownSprite, Vector2.zero, new Vector2(150, 203));
            EnsureStretch(lockedImage.gameObject);
            lockedImage.raycastTarget = false;

            var soilImage = EnsureImageChild(slot.transform, "SoilImage", uiLayer, soilSprite,
                new Vector2(0, -10), new Vector2(98, 48));
            ApplyRect(soilImage.gameObject, new Vector2(0, -10), new Vector2(98, 48));
            soilImage.raycastTarget = false;
            soilImage.enabled = false;
            soilImage.gameObject.SetActive(false);

            var flowerBindings = BuildBloomedFlowerBindings();
            var flowerPreview = flowerBindings.Count > 0 ? flowerBindings[0].Value : null;
            var flowerImage = EnsureImageChild(slot.transform, "FlowerImage", uiLayer, flowerPreview, new Vector2(0, 18), new Vector2(98, 98));
            ApplyRect(flowerImage.gameObject, new Vector2(0, 18), new Vector2(98, 98));
            flowerImage.color = Color.white;
            flowerImage.raycastTarget = false;
            string flowerPreviewKey = ResolveCodexScenePreviewKey(index, flowerBindings);
            bool showSceneUnlockedPreview = !string.IsNullOrEmpty(flowerPreviewKey);
            ConfigureImageVariantView(flowerImage.gameObject, flowerImage, uiLayer, flowerBindings,
                flowerPreviewKey, showSceneUnlockedPreview);
            soilImage.transform.SetSiblingIndex(flowerImage.transform.GetSiblingIndex());

            var unlockedContent = EnsureFullRect(slot.transform, "UnlockedContent", uiLayer);
            var numberText = EnsureTextChild(unlockedContent.transform, uiLayer, "NumberText", "No. 027", 16,
                new Vector2(0, 82), new Vector2(126, 26), new Color(0.35f, 0.2f, 0.12f, 1f), TextAlignmentOptions.Center);
            numberText.fontStyle = FontStyles.Normal;
            var nameText = EnsureTextChild(unlockedContent.transform, uiLayer, "NameText", "情绪之花", 18,
                new Vector2(0, -62), new Vector2(132, 32), new Color(0.33f, 0.18f, 0.1f, 1f), TextAlignmentOptions.Center);
            nameText.fontStyle = FontStyles.Bold;
            EnsureTextChild(unlockedContent.transform, uiLayer, "MetaText", "培育者 · 0", 14,
                new Vector2(0, -88), new Vector2(132, 26), new Color(0.45f, 0.28f, 0.17f, 1f), TextAlignmentOptions.Center);

            // Scene 默认保存前三朵已收集花，便于直接校对图鉴列表与详情页的真实花枝。
            // 其余卡片保持锁定态；运行时只在这些预置节点之间切换，不生成视觉对象。
            bool showSceneUnlockedCard = showSceneUnlockedPreview;
            lockedImage.gameObject.SetActive(!showSceneUnlockedCard);
            flowerImage.enabled = showSceneUnlockedCard;
            flowerImage.gameObject.SetActive(showSceneUnlockedCard);
            soilImage.enabled = showSceneUnlockedCard;
            soilImage.gameObject.SetActive(showSceneUnlockedCard);
            unlockedContent.SetActive(showSceneUnlockedCard);
            if (showSceneUnlockedCard)
            {
                numberText.text = $"No. {FirstCodexFlowerNumber + index:000}";
                string sceneEmotion = ResolveCodexSceneEmotion(index);
                nameText.text = EmotionFlowerCatalog.ResolveFlowerName(sceneEmotion, EmotionFlowerCatalog.OwnerAngel);
                var sceneOwner = EmotionFlowerCatalog.ResolveOwnerDisplayName(EmotionFlowerCatalog.OwnerAngel);
                unlockedContent.transform.Find("MetaText")?.GetComponent<TextMeshProUGUI>()?.SetText(
                    $"{sceneOwner} · 1");
            }

            return slot;
        }

        private static string ResolveCodexSceneEmotion(int index)
        {
            return index switch
            {
                0 => "喜悦",
                1 => "悲伤",
                2 => "平静",
                _ => string.Empty
            };
        }

        private static string ResolveCodexScenePreviewKey(int index,
            IReadOnlyList<KeyValuePair<string, Sprite>> bindings)
        {
            string emotion = ResolveCodexSceneEmotion(index);
            if (string.IsNullOrEmpty(emotion))
            {
                return string.Empty;
            }

            string key = SceneAuthoredImageVariantView.BuildFlowerKey(
                emotion, EmotionFlowerCatalog.OwnerAngel, GrowthState.Bloomed);
            for (int i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].Key, key, System.StringComparison.Ordinal))
                {
                    return key;
                }
            }

            return string.Empty;
        }

        private static void EnsureStretch(GameObject go)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void BindCardSlot(SerializedProperty slotProp, GameObject slot)
        {
            slotProp.FindPropertyRelative("_button").objectReferenceValue = slot.GetComponent<Button>();
            slotProp.FindPropertyRelative("_cardImage").objectReferenceValue = slot.GetComponent<Image>();
            slotProp.FindPropertyRelative("_lockedImage").objectReferenceValue = slot.transform.Find("LockedImage")?.GetComponent<Image>();
            slotProp.FindPropertyRelative("_flowerView").objectReferenceValue = slot.transform.Find("FlowerImage")?.GetComponent<SceneAuthoredImageVariantView>();
            slotProp.FindPropertyRelative("_soilImage").objectReferenceValue = slot.transform.Find("SoilImage")?.GetComponent<Image>();
            slotProp.FindPropertyRelative("_unlockedContent").objectReferenceValue = slot.transform.Find("UnlockedContent")?.gameObject;
            slotProp.FindPropertyRelative("_numberText").objectReferenceValue = slot.transform.Find("UnlockedContent/NumberText")?.GetComponent<TextMeshProUGUI>();
            slotProp.FindPropertyRelative("_nameText").objectReferenceValue = slot.transform.Find("UnlockedContent/NameText")?.GetComponent<TextMeshProUGUI>();
            slotProp.FindPropertyRelative("_metaText").objectReferenceValue = slot.transform.Find("UnlockedContent/MetaText")?.GetComponent<TextMeshProUGUI>();
        }

        private static void SetupFlowerCollectionContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<FlowerCollectionPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;

            EnsureChildText(contentT, uiLayer, "CollectionTitle", "情绪花图鉴", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(400, 46));

            // ScrollView
            var scrollGo = EnsureChild(contentT, "ScrollView", uiLayer);
            ScrollRect scrollRect;
            RectTransform vpRt;
            RectTransform lRt;
            bool isNew = scrollGo.transform.childCount == 0;

            if (isNew)
            {
                var svRt = scrollGo.AddComponent<RectTransform>();
                svRt.anchorMin = new Vector2(0.5f, 0.5f);
                svRt.anchorMax = new Vector2(0.5f, 0.5f);
                svRt.pivot = new Vector2(0.5f, 0.5f);
                svRt.anchoredPosition = new Vector2(0, -40);
                svRt.sizeDelta = new Vector2(600, 520);
                var svImg = scrollGo.AddComponent<Image>();
                svImg.color = new Color(1f, 1f, 1f, 0.05f);
            }
            else
            {
                if (scrollGo.GetComponent<RectTransform>() == null) scrollGo.AddComponent<RectTransform>();
                if (scrollGo.GetComponent<Image>() == null) scrollGo.AddComponent<Image>();
            }
            scrollRect = GetOrAdd<ScrollRect>(scrollGo);

            // Viewport
            var viewportGo = EnsureChild(scrollGo.transform, "Viewport", uiLayer);
            if (viewportGo.transform.childCount == 0)
            {
                vpRt = viewportGo.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.sizeDelta = Vector2.zero;
                vpRt.pivot = new Vector2(0, 0.5f);
                var vpImg = viewportGo.AddComponent<Image>();
                vpImg.color = Color.clear;
                var vpMask = viewportGo.AddComponent<Mask>();
                vpMask.showMaskGraphic = false;
            }
            else
            {
                vpRt = GetOrAdd<RectTransform>(viewportGo);
                if (viewportGo.GetComponent<Image>() == null) viewportGo.AddComponent<Image>();
                if (viewportGo.GetComponent<Mask>() == null) viewportGo.AddComponent<Mask>();
            }

            // 遮罩图形必须不透明：全透明 Image 会被 CanvasRenderer 剔除，
            // Mask 写不进 stencil，列表整体不可见（showMaskGraphic=false 已保证它不显示）
            var vpImgFix = GetOrAdd<Image>(viewportGo);
            vpImgFix.color = Color.white;
            var vpMaskFix = GetOrAdd<Mask>(viewportGo);
            vpMaskFix.showMaskGraphic = false;

            // Content (_listRoot)
            var listGo = EnsureChild(viewportGo.transform, "ListRoot", uiLayer);
            if (listGo == null) return;
            lRt = GetOrAdd<RectTransform>(listGo);
            if (lRt == null) return;
            // ListRoot 的子物体是运行时生成的，Editor 下 childCount 恒为 0，
            // 因此这里必须全程 get-or-add，属性重复设置是幂等的
            lRt.anchorMin = new Vector2(0, 1);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.pivot = new Vector2(0.5f, 1);
            lRt.sizeDelta = new Vector2(0, 0);
            var vl = GetOrAdd<VerticalLayoutGroup>(listGo);
            vl.spacing = 8;
            vl.padding = new RectOffset(12, 12, 12, 12);
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            var fitter = GetOrAdd<ContentSizeFitter>(listGo);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRt;
            scrollRect.content = lRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 旧的"立即开花"调试按钮已被输入面板的"进入下一天"取代
            var oldBloomBtn = contentT.Find("DebugBloomBtn");
            if (oldBloomBtn != null) Object.DestroyImmediate(oldBloomBtn.gameObject);

            so.FindProperty("_listRoot").objectReferenceValue = listGo.transform;
            so.ApplyModifiedProperties();
        }

        // ── UI 元素工厂（仅首次创建设置属性）──────────────

        private static TMP_InputField EnsureInputField(Transform parent, int uiLayer)
        {
            var existing = parent.Find("InputField");
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                // 已存在：只确保组件齐全 + 连线
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
                var field = GetOrAdd<TMP_InputField>(go);
                field.lineType = TMP_InputField.LineType.MultiLineNewline;
                field.interactable = true;
                field.targetGraphic = go.GetComponent<Image>();

                var textArea = EnsureChild(go.transform, "Text Area", uiLayer);
                if (textArea != null)
                {
                    if (textArea.GetComponent<RectTransform>() == null) textArea.AddComponent<RectTransform>();
                    var placeholder = EnsureChild(textArea.transform, "Placeholder", uiLayer);
                    if (placeholder != null && placeholder.GetComponent<TextMeshProUGUI>() == null)
                        placeholder.AddComponent<TextMeshProUGUI>();
                    var text = EnsureChild(textArea.transform, "Text", uiLayer);
                    if (text != null && text.GetComponent<TextMeshProUGUI>() == null)
                        text.AddComponent<TextMeshProUGUI>();
                    field.textViewport = textArea.GetComponent<RectTransform>();
                    field.placeholder = placeholder?.GetComponent<TextMeshProUGUI>();
                    field.textComponent = text?.GetComponent<TextMeshProUGUI>();
                }

                return field;
            }

            // 首次创建
            go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var irt = go.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, 40);
            irt.sizeDelta = new Vector2(500, 140);
            var inputImg = go.AddComponent<Image>();
            inputImg.color = new Color(1f, 1f, 1f, 0.08f);
            var inputField = go.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.interactable = true;
            inputField.targetGraphic = inputImg;

            var textAreaNew = EnsureChild(go.transform, "Text Area", uiLayer);
            textAreaNew.AddComponent<RectTransform>();
            var placeholderNew = EnsureChild(textAreaNew.transform, "Placeholder", uiLayer);
            var phTmp = placeholderNew.AddComponent<TextMeshProUGUI>();
            phTmp.text = "输入你今天的心情…";
            phTmp.fontSize = 18;
            phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            inputField.placeholder = phTmp;

            var textNew = EnsureChild(textAreaNew.transform, "Text", uiLayer);
            var tTmp = textNew.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize = 18;
            tTmp.color = Color.white;
            inputField.textComponent = tTmp;
            inputField.textViewport = textAreaNew.GetComponent<RectTransform>();

            return inputField;
        }

        private static Button EnsureButtonWithLabel(Transform parent, int uiLayer, string name, string label,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var existing = parent.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
                var btn = GetOrAdd<Button>(go);

                var labelChild = EnsureChild(go.transform, "Label", uiLayer);
                if (labelChild.GetComponent<RectTransform>() == null) labelChild.AddComponent<RectTransform>();
                if (labelChild.GetComponent<TextMeshProUGUI>() == null) labelChild.AddComponent<TextMeshProUGUI>();
                return btn;
            }

            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var img = go.AddComponent<Image>();
            img.color = color;
            var button = go.AddComponent<Button>();

            var labelGo = EnsureChild(go.transform, "Label", uiLayer);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return button;
        }

        // ── 按钮 ──────────────────────────────────────────────

        private static Button CreatePanelButton(GameObject canvas, int uiLayer, string name, string label, int index)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-32 - index * 170, -120);
            rt.sizeDelta = new Vector2(160, 52);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return btn;
        }

        // ── 7/20 修改清单 ──────────────────────────────────────

        /// <summary>左右下角箭头按钮：点击滚动场景，摄像机不跟桌宠。</summary>
        private static void EnsureArrowButtons(GameObject canvasGo, int uiLayer)
        {
            var existingLeft = canvasGo.transform.Find("Btn_ScrollLeft");
            var existingRight = canvasGo.transform.Find("Btn_ScrollRight");

            if (existingLeft != null && existingRight != null) return;

            var cam = UnityEngine.Object.FindFirstObjectByType<WorldMapCameraController>();
            if (cam == null)
            {
                Debug.LogWarning("[WorldMapEmotionGardenUI] 未找到 WorldMapCameraController，跳过箭头按钮创建");
                return;
            }

            if (existingLeft == null)
            {
                var go = new GameObject("Btn_ScrollLeft");
                go.transform.SetParent(canvasGo.transform, false);
                go.layer = uiLayer;
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(24, 0);
                rt.sizeDelta = new Vector2(56, 80);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.12f, 0.14f, 0.22f, 0.75f);
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;

                var arrowGo = new GameObject("Arrow");
                arrowGo.transform.SetParent(go.transform, false);
                arrowGo.layer = uiLayer;
                var art = arrowGo.AddComponent<RectTransform>();
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
                art.offsetMin = Vector2.zero; art.offsetMax = Vector2.zero;
                var tmp = arrowGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "◀";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28;
                tmp.color = Color.white;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, cam.ScrollLeft);

                // hover 变色
                var colors = btn.colors;
                colors.normalColor = new Color(0.12f, 0.14f, 0.22f, 0.75f);
                colors.highlightedColor = new Color(0.22f, 0.28f, 0.45f, 0.9f);
                btn.colors = colors;
            }

            if (existingRight == null)
            {
                var go = new GameObject("Btn_ScrollRight");
                go.transform.SetParent(canvasGo.transform, false);
                go.layer = uiLayer;
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-24, 0);
                rt.sizeDelta = new Vector2(56, 80);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.12f, 0.14f, 0.22f, 0.75f);
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;

                var arrowGo = new GameObject("Arrow");
                arrowGo.transform.SetParent(go.transform, false);
                arrowGo.layer = uiLayer;
                var art = arrowGo.AddComponent<RectTransform>();
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
                art.offsetMin = Vector2.zero; art.offsetMax = Vector2.zero;
                var tmp = arrowGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "▶";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28;
                tmp.color = Color.white;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, cam.ScrollRight);

                var colors = btn.colors;
                colors.normalColor = new Color(0.12f, 0.14f, 0.22f, 0.75f);
                colors.highlightedColor = new Color(0.22f, 0.28f, 0.45f, 0.9f);
                btn.colors = colors;
            }

            // 禁用摄像机跟随桌宠（通过 SerializedObject 设置私有字段）
            var camSo = new SerializedObject(cam);
            var fp = camSo.FindProperty("_followSelectedPet");
            if (fp != null && fp.boolValue)
            {
                fp.boolValue = false;
                camSo.ApplyModifiedProperties();
            }
        }

        /// <summary>将 Cabin 场景物改造为返回公寓入口（替换 ClickableSceneObject → CabinReturnPortal + 移除旧返回按钮）。</summary>
        private static void EnsureCabinReturnPortal()
        {
            var cabin = GameObject.Find("室内");
            if (cabin == null)
            {
                Debug.LogWarning("[WorldMapEmotionGardenUI] 未找到「室内」（小木屋入口）");
                return;
            }

            // 移除旧的 ClickableSceneObject
            var oldClickable = cabin.GetComponent<ClickableSceneObject>();
            if (oldClickable != null) Object.DestroyImmediate(oldClickable);

            // 确保 CabinReturnPortal
            if (cabin.GetComponent<CabinReturnPortal>() == null)
            {
                var portal = cabin.AddComponent<CabinReturnPortal>();
                var so = new SerializedObject(portal);
                so.ApplyModifiedProperties();
            }

            // 确保 Collider2D 非 trigger（OnMouseEnter/Exit 需要）
            var col = cabin.GetComponent<Collider2D>();
            if (col != null && col.isTrigger)
            {
                col.isTrigger = false;
            }

            // 移除旧版 Canvas 上的返回公寓按钮
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var oldBtn = canvas.transform.Find("Btn_ReturnApartment");
                if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

                // 也清理 WorldMapExit 旧节点（如果仍存在）
                var oldExit = GameObject.Find("WorldMapExit");
                if (oldExit != null) Object.DestroyImmediate(oldExit);
            }

            Debug.Log("[WorldMapEmotionGardenUI] Cabin → 返回公寓入口（hover + 点击）");
        }

        /// <summary>隐藏九宫格花圃（当前无数据，先隐藏）。</summary>
        private static void HideGardenPlots()
        {
            var plots = GameObject.Find("GardenPlots");
            if (plots != null && plots.activeSelf)
            {
                plots.SetActive(false);
                Debug.Log("[WorldMapEmotionGardenUI] GardenPlots 已隐藏");
            }
        }

        /// <summary>确保摄像机 _followSelectedPet = false。</summary>
        private static void DisableCameraFollow()
        {
            var cam = UnityEngine.Object.FindFirstObjectByType<WorldMapCameraController>();
            if (cam == null) return;

            var so = new SerializedObject(cam);
            var prop = so.FindProperty("_followSelectedPet");
            if (prop != null && prop.boolValue)
            {
                prop.boolValue = false;
                so.ApplyModifiedProperties();
            }
        }

        // ── helpers ────────────────────────────────────────────

        private static GameObject EnsureChild(Transform parent, string name, int uiLayer)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            return go;
        }

        /// <summary>不能写 GetComponent ?? AddComponent：Editor 下 GetComponent 对缺失组件返回假 null，?? 不会触发 AddComponent。</summary>
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static GameObject EnsureChildText(Transform parent, int uiLayer, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.GetComponent<TextMeshProUGUI>() == null)
                    existing.gameObject.AddComponent<TextMeshProUGUI>();
                return existing.gameObject;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }
    }
}
#endif
