#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 为 WorldMap 创建花朵自由摆放的右侧侧边栏和 Scene 作者化摆放对象。
    /// 运行时不创建 UI、Sprite、网格线或落位物体，只切换本文件创建的预置节点。
    /// </summary>
    public static class WorldMapFlowerPlacementAuthoring
    {
        private const string FlowerSortingLayerName = "Default";

        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string ArrangeArtDir = "Assets/_Project/Art/WorldMap/arrange";
        private const string FlowerCodexArtDir = "Assets/_Project/Art/WorldMap/花朵图鉴";
        private const string PlacementSingleArtDir = FlowerCodexArtDir + "/花朵放置";
        private const string GridMaterialPath = ArrangeArtDir + "/PlacementGrid.mat";
        private const string GridReferencePath = "Assets/_Project/Art/WorldMap/garden/中景/花丛.png";
        private const int PlacementSlotCount = 32;

        private static readonly string[] EmotionTypes =
        {
            "喜悦", "悲伤", "愤怒", "平静", "爱", "恐惧", "惊讶", "期待", "孤独"
        };

        [MenuItem("Tools/Gemini-Lab/WorldMap/Author Flower Placement")]
        public static void PatchScene()
        {
            if (!Application.isBatchMode &&
                EditorSceneManager.GetActiveScene().path != ScenePath &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[WorldMapFlowerPlacement] 未找到 WorldMap Canvas。");
                return;
            }

            Patch(canvasGo, canvasGo.layer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WorldMapFlowerPlacement] 独立场景作者化完成。");
        }

        public static void Patch(GameObject canvasGo, int uiLayer)
        {
            RemoveLegacyPlacementUi(canvasGo.transform);

            var controller = canvasGo.GetComponent<WorldMapFlowerPlacementController>();
            if (controller == null) controller = canvasGo.AddComponent<WorldMapFlowerPlacementController>();

            var openButton = EnsureAnchoredButton(canvasGo.transform, uiLayer, "Btn_FlowerPlacement", "布置",
                new Vector2(-28f, 28f), new Vector2(150f, 52f),
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Color(0.16f, 0.36f, 0.24f, 0.96f));

            var panel = EnsureSidebarPanel(canvasGo.transform, uiLayer);
            var closeButton = EnsureAnchoredButton(panel.transform, uiLayer, "CloseButton", "×",
                new Vector2(-34f, -30f), new Vector2(46f, 46f),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Color(0.36f, 0.22f, 0.12f, 0.94f));

            var title = EnsureTextChild(panel.transform, "SidebarTitle", uiLayer, "自由摆放",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(300f, 44f), 26f);
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.32f, 0.18f, 0.08f, 1f);

            var scroll = EnsureScrollView(panel.transform, uiLayer);
            var content = scroll.content!;
            var list = EnsureFlowerList(content.transform, uiLayer);
            // FlowerList 是滚动内容和唯一的纵向条目布局根。
            // FlowerSidebarContent 仅保留为场景层级容器，不再参与尺寸回算。
            scroll.content = list.GetComponent<RectTransform>();
            var hintBubble = EnsureHintBubble(panel.transform, uiLayer);
            var statusText = EnsureTextChild(panel.transform, "PlacementStatus", uiLayer, string.Empty,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 50f), new Vector2(380f, 34f), 15f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = new Color(0.36f, 0.23f, 0.12f, 1f);

            var root = GameObject.Find("WorldMapPlacedFlowers");
            if (root == null)
            {
                root = new GameObject("WorldMapPlacedFlowers");
                root.transform.position = Vector3.zero;
            }

            var surface = EnsurePlacementBounds();
            var regions = EnsurePlacementRegions(root.transform, surface);
            var grid = EnsurePlacementGrid(root.transform);
            var material = EnsureGridMaterial();
            ConfigureGrid(grid, surface, material);
            ConfigureFixedBaselineDefinitions(grid, surface, material);
            var previewRoot = EnsurePreviewRoot(root.transform);
            var previewBindings = ConfigurePreview(previewRoot, uiLayer);
            var slots = ConfigurePlacementSlots(root.transform, uiLayer);

            var so = new SerializedObject(controller);
            SetObject(so, "_openButton", openButton);
            SetObject(so, "_closeButton", closeButton);
            SetObject(so, "_sidebarPanel", panel);
            SetObject(so, "_sidebarScrollRect", scroll);
            SetObject(so, "_flowerListLayoutRoot", list.GetComponent<RectTransform>());
            SetObject(so, "_placementGrid", grid);
            SetObject(so, "_previewRoot", previewRoot);
            SetObject(so, "_placementRoot", root.transform);
            SetObject(so, "_placementSurface", surface);
            ConfigurePlacementRegions(so, regions);
            SetObject(so, "_statusText", statusText);
            SetObject(so, "_hintBubble", hintBubble);
            so.FindProperty("_gridOrigin")!.vector2Value = Vector2.zero;
            so.FindProperty("_cellSize")!.vector2Value = LoadGridCellSize();
            so.FindProperty("_clusterCellSize")!.vector2Value = LoadClusterCellSize();
            so.FindProperty("_baselineSnapTolerance")!.floatValue = 0.05f;
            so.FindProperty("_flowerSortingOrderOffset")!.intValue = 0;
            so.FindProperty("_flowerSortingLayerName")!.stringValue = FlowerSortingLayerName;
            EnsureWorldMapPetBaselines();
            ConfigurePlacementLayers(so, surface);
            so.FindProperty("_useSurfaceBoundsAsGridOrigin")!.boolValue = true;
            ConfigurePreviewBindings(so, previewBindings);
            ConfigurePlacementSlotReferences(so, slots);
            ConfigureFlowerOptions(so, list.transform, uiLayer);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            closeButton.gameObject.SetActive(false);
            grid.SetActive(false);
            previewRoot.SetActive(false);
            for (int i = 0; i < slots.Count; i++)
                slots[i].gameObject.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[WorldMapFlowerPlacement] 已作者化右侧侧边栏、18 种花卉资源、网格、预览和落位对象池");
        }

        private static GameObject EnsureSidebarPanel(Transform canvas, int layer)
        {
            var panel = EnsureChild(canvas, "FlowerPlacementPanel", layer);
            // UIBoard 原始尺寸为 498 × 899，保持右侧原图比例，避免纵向拉伸。
            ConfigureRect(panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(498f, 899f), new Vector2(1f, 0.5f));
            var image = GetOrAdd<Image>(panel);
            image.sprite = LoadSprite(ArrangeArtDir + "/UIBoard.png");
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            return panel;
        }

        private static void RemoveLegacyPlacementUi(Transform canvas)
        {
            Transform? legacyStatusBar = canvas.Find("FlowerPlacementStatusBar");
            if (legacyStatusBar != null)
                UnityEngine.Object.DestroyImmediate(legacyStatusBar.gameObject);

            // These four controls belonged to the first bottom-bar prototype.
            // The current sidebar owns its buttons inside FlowerOption entries;
            // leaving the old nodes under FlowerPlacementPanel makes them appear
            // as the grey placeholder controls seen in Play mode.
            Transform? panel = canvas.Find("FlowerPlacementPanel");
            if (panel == null) return;

            foreach (string name in new[]
                     {
                         "FlowerButtons",
                         "SingleButton",
                         "ClusterButton",
                         "CancelButton"
                     })
            {
                Transform? legacyNode = panel.Find(name);
                if (legacyNode != null)
                    UnityEngine.Object.DestroyImmediate(legacyNode.gameObject);
            }
        }

        private static ScrollRect EnsureScrollView(Transform panel, int layer)
        {
            var viewport = EnsureChild(panel, "FlowerSidebarViewport", layer);
            ConfigureRect(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var viewportRect = viewport.GetComponent<RectTransform>()!;
            // 拉伸锚点下必须使用 offsetMin / offsetMax 表达四边距。
            // 列表顶部固定在标题下方，底部也留出装饰边框，防止条目越出窗口。
            viewportRect.offsetMin = new Vector2(34f, 56f);
            viewportRect.offsetMax = new Vector2(-34f, -132f);
            GetOrAdd<RectMask2D>(viewport);

            var content = EnsureChild(viewport.transform, "FlowerSidebarContent", layer);
            ConfigureRect(content, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            RemoveComponent<VerticalLayoutGroup>(content);
            RemoveComponent<ContentSizeFitter>(content);

            var scroll = GetOrAdd<ScrollRect>(viewport);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            scroll.inertia = true;
            return scroll;
        }

        private static GameObject EnsureFlowerList(Transform content, int layer)
        {
            var list = EnsureChild(content, "FlowerList", layer);
            ConfigureRect(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(398f, 0f), new Vector2(0.5f, 1f));
            var vertical = GetOrAdd<VerticalLayoutGroup>(list);
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            // 必须由 FlowerList 按每个条目的 LayoutElement（收起 73 / 展开 320）设置真实高度，
            // 否则详情虽然显示，但后续条目仍按旧 73 高度排布并遮挡详情。
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            var fitter = GetOrAdd<ContentSizeFitter>(list);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return list;
        }

        private static GameObject EnsureHintBubble(Transform panel, int layer)
        {
            var hint = EnsureChild(panel, "SynthesisHintBubble", layer);
            // 右侧面板的合成提示应向左弹出，不能继续越过右侧屏幕边界。
            ConfigureRect(hint, new Vector2(0f, 0.42f), new Vector2(0f, 0.42f),
                new Vector2(-12f, 0f), new Vector2(286f, 151f), new Vector2(1f, 0.5f));
            var image = GetOrAdd<Image>(hint);
            image.sprite = LoadSprite(ArrangeArtDir + "/组 5.png");
            image.color = Color.white;
            image.preserveAspect = true;
            hint.SetActive(false);
            return hint;
        }

        private static void ConfigureFlowerOptions(SerializedObject controller, Transform list, int layer)
        {
            var options = controller.FindProperty("_options")!;
            int optionCount = EmotionTypes.Length * 2;
            RemoveObsoleteFlowerOptions(list, optionCount);
            options.arraySize = optionCount;

            for (int i = 0; i < optionCount; i++)
            {
                int emotionIndex = i / 2;
                string owner = i % 2 == 0 ? EmotionFlowerCatalog.OwnerAngel : EmotionFlowerCatalog.OwnerDemon;
                string emotion = EmotionTypes[emotionIndex];
                string ownerDisplay = EmotionFlowerCatalog.ResolveOwnerDisplayName(owner);
                string id = owner + "|" + emotion;
                string displayName = EmotionFlowerCatalog.ResolveFlowerName(emotion, owner);

                var entry = EnsureFlowerEntry(list, layer, i, displayName,
                    LoadSprite(FlowerCodexArtDir + "/花朵/" + ownerDisplay + "-" + emotion + ".PNG"),
                    LoadSprite(PlacementSingleArtDir + "/" + ownerDisplay + "-" + emotion + ".PNG"),
                    LoadSprite(FlowerCodexArtDir + "/花丛/" + ownerDisplay + "-" + emotion + "（花丛）.PNG"));

                var option = options.GetArrayElementAtIndex(i);
                option.FindPropertyRelative("_id")!.stringValue = id;
                option.FindPropertyRelative("_displayName")!.stringValue = displayName;
                option.FindPropertyRelative("_initialSingleCount")!.intValue = 0;
                option.FindPropertyRelative("_initialClusterCount")!.intValue = 0;
                option.FindPropertyRelative("_singleFootprint")!.vector2IntValue = Vector2Int.one;
                option.FindPropertyRelative("_clusterFootprint")!.vector2IntValue = Vector2Int.one;
                option.FindPropertyRelative("_collapsedHeight")!.floatValue = 73f;
                option.FindPropertyRelative("_expandedHeight")!.floatValue = 320f;

                SetObject(option, "_entryLayout", entry.EntryLayout);
                SetObject(option, "_headerButton", entry.HeaderButton);
                SetObject(option, "_displayNameText", entry.DisplayNameText);
                SetObject(option, "_expandedRoot", entry.ExpandedRoot);
                SetObject(option, "_arrowUp", entry.ArrowUp);
                SetObject(option, "_arrowDown", entry.ArrowDown);
                SetObject(option, "_singleButton", entry.SingleButton);
                SetObject(option, "_clusterButton", entry.ClusterButton);
                SetObject(option, "_synthesisButton", entry.SynthesisButton);
                SetObject(option, "_singleCountText", entry.SingleCountText);
                SetObject(option, "_clusterCountText", entry.ClusterCountText);
                SetObject(option, "_singleSelectedMark", entry.SingleSelectedMark);
                SetObject(option, "_clusterSelectedMark", entry.ClusterSelectedMark);
            }
        }

        private static void RemoveObsoleteFlowerOptions(Transform list, int currentOptionCount)
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                Transform child = list.GetChild(i);
                const string prefix = "FlowerOption_";
                if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;

                string indexText = child.name.Substring(prefix.Length);
                bool isCurrentName = indexText.Length == 2 &&
                                     int.TryParse(indexText, out int index) &&
                                     index >= 0 &&
                                     index < currentOptionCount;
                if (!isCurrentName)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private sealed class FlowerEntryResult
        {
            public LayoutElement EntryLayout = null!;
            public Button HeaderButton = null!;
            public TMP_Text DisplayNameText = null!;
            public GameObject ExpandedRoot = null!;
            public GameObject ArrowUp = null!;
            public GameObject ArrowDown = null!;
            public Button SingleButton = null!;
            public Button ClusterButton = null!;
            public Button SynthesisButton = null!;
            public TMP_Text SingleCountText = null!;
            public TMP_Text ClusterCountText = null!;
            public GameObject SingleSelectedMark = null!;
            public GameObject ClusterSelectedMark = null!;
        }

        private static FlowerEntryResult EnsureFlowerEntry(
            Transform list,
            int layer,
            int index,
            string displayName,
            Sprite? flowerIcon,
            Sprite? singleSprite,
            Sprite? clusterSprite)
        {
            var entry = EnsureChild(list, "FlowerOption_" + index.ToString("00"), layer);
            ConfigureRect(entry, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(398f, 73f), new Vector2(0.5f, 1f));
            RemoveComponent<VerticalLayoutGroup>(entry);
            RemoveComponent<ContentSizeFitter>(entry);
            var entryLayout = GetOrAdd<LayoutElement>(entry);
            entryLayout.minWidth = 398f;
            entryLayout.preferredWidth = 398f;
            entryLayout.flexibleWidth = 0f;
            entryLayout.minHeight = 73f;
            entryLayout.preferredHeight = 73f;
            entryLayout.flexibleHeight = 0f;

            var header = EnsureChild(entry.transform, "Header", layer);
            ConfigureRect(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(398f, 73f), new Vector2(0.5f, 1f));
            var headerLayout = GetOrAdd<LayoutElement>(header);
            headerLayout.preferredWidth = 398f;
            headerLayout.preferredHeight = 73f;
            var headerImage = GetOrAdd<Image>(header);
            headerImage.sprite = LoadSprite(ArrangeArtDir + "/item.png");
            headerImage.color = Color.white;
            headerImage.preserveAspect = true;
            var headerButton = GetOrAdd<Button>(header);
            headerButton.targetGraphic = headerImage;

            var icon = EnsureImageChild(header.transform, "FlowerIcon", layer, flowerIcon,
                new Vector2(0f, 0.5f), new Vector2(58f, 58f), new Vector2(20f, 0f));
            icon.preserveAspect = true;
            var nameText = EnsureTextChild(header.transform, "FlowerName", layer, displayName,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(166f, 0f), new Vector2(214f, 42f), 21f);
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = new Color(0.34f, 0.20f, 0.10f, 1f);

            var arrowUp = EnsureImageChild(header.transform, "ArrowUp", layer,
                LoadSprite(ArrangeArtDir + "/keyboard_arrow_up.png"),
                new Vector2(1f, 0.5f), new Vector2(28f, 28f), new Vector2(-38f, 0f));
            var arrowDown = EnsureImageChild(header.transform, "ArrowDown", layer,
                LoadSprite(ArrangeArtDir + "/keyboard_arrow_down.png"),
                new Vector2(1f, 0.5f), new Vector2(28f, 28f), new Vector2(-38f, 0f));
            arrowUp.preserveAspect = true;
            arrowDown.preserveAspect = true;
            arrowUp.gameObject.SetActive(false);

            var expanded = EnsureChild(entry.transform, "ExpandedOptions", layer);
            ConfigureRect(expanded, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -76f), new Vector2(398f, 244f), new Vector2(0.5f, 1f));
            var expandedVertical = GetOrAdd<VerticalLayoutGroup>(expanded);
            expandedVertical.spacing = 6f;
            expandedVertical.childAlignment = TextAnchor.UpperCenter;
            expandedVertical.childControlWidth = true;
            expandedVertical.childControlHeight = false;
            expandedVertical.childForceExpandWidth = true;
            expandedVertical.childForceExpandHeight = false;
            RemoveComponent<ContentSizeFitter>(expanded);
            expanded.SetActive(false);

            var cards = EnsureChild(expanded.transform, "VariantCards", layer);
            ConfigureRect(cards, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(336f, 180f), new Vector2(0.5f, 1f));
            var cardsElement = GetOrAdd<LayoutElement>(cards);
            cardsElement.preferredWidth = 336f;
            cardsElement.preferredHeight = 180f;
            var cardLayout = GetOrAdd<HorizontalLayoutGroup>(cards);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlWidth = false;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            var single = EnsureVariantCard(cards.transform, layer, "SingleCard", "单花",
                LoadSprite(ArrangeArtDir + "/singleCard.png"), singleSprite);
            var cluster = EnsureVariantCard(cards.transform, layer, "ClusterCard", "花丛",
                LoadSprite(ArrangeArtDir + "/tripleCard.png"), clusterSprite);
            var synthesis = EnsureButton(expanded.transform, layer, "SynthesisButton", string.Empty,
                new Vector2(0f, 0f), new Vector2(171f, 51f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                Color.white);
            var synthesisImage = synthesis.GetComponent<Image>();
            if (synthesisImage != null)
            {
                synthesisImage.sprite = LoadSprite(ArrangeArtDir + "/synthesis.png");
                synthesisImage.preserveAspect = true;
            }
            var synthesisLayout = GetOrAdd<LayoutElement>(synthesis.gameObject);
            synthesisLayout.preferredWidth = 171f;
            synthesisLayout.preferredHeight = 51f;

            return new FlowerEntryResult
            {
                EntryLayout = entryLayout,
                HeaderButton = headerButton,
                DisplayNameText = nameText,
                ExpandedRoot = expanded,
                ArrowUp = arrowUp.gameObject,
                ArrowDown = arrowDown.gameObject,
                SingleButton = single.Button,
                ClusterButton = cluster.Button,
                SynthesisButton = synthesis,
                SingleCountText = single.CountText,
                ClusterCountText = cluster.CountText,
                SingleSelectedMark = single.SelectedMark,
                ClusterSelectedMark = cluster.SelectedMark
            };
        }

        private sealed class VariantCardResult
        {
            public Button Button = null!;
            public TMP_Text CountText = null!;
            public GameObject SelectedMark = null!;
        }

        private static VariantCardResult EnsureVariantCard(
            Transform parent,
            int layer,
            string name,
            string label,
            Sprite? backgroundSprite,
            Sprite? iconSprite)
        {
            var card = EnsureChild(parent, name, layer);
            ConfigureRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(159f, 180f), new Vector2(0.5f, 0.5f));
            var layout = GetOrAdd<LayoutElement>(card);
            layout.preferredWidth = 159f;
            layout.preferredHeight = 180f;
            var image = GetOrAdd<Image>(card);
            image.sprite = backgroundSprite;
            image.color = Color.white;
            image.preserveAspect = true;
            var button = GetOrAdd<Button>(card);
            button.targetGraphic = image;

            var icon = EnsureImageChild(card.transform, "OptionIcon", layer, iconSprite,
                new Vector2(0.5f, 0.5f), new Vector2(112f, 112f), new Vector2(0f, 16f));
            icon.preserveAspect = true;
            var count = EnsureTextChild(card.transform, "CountText", layer, "×0",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(110f, 28f), 18f);
            count.alignment = TextAlignmentOptions.Center;
            count.color = new Color(0.34f, 0.20f, 0.10f, 1f);

            var selected = EnsureImageChild(card.transform, "SelectedMark", layer,
                LoadSprite(ArrangeArtDir + "/tick.png"),
                new Vector2(1f, 1f), new Vector2(44f, 44f), new Vector2(-10f, -10f));
            selected.preserveAspect = true;
            selected.gameObject.SetActive(false);

            return new VariantCardResult
            {
                Button = button,
                CountText = count,
                SelectedMark = selected.gameObject
            };
        }

        private static List<WorldMapFlowerPlacementController.PlacementVisualBinding> ConfigurePreview(
            GameObject previewRoot,
            int uiLayer)
        {
            RemoveChildrenWithPrefix(previewRoot.transform, "PreviewVisual_");
            var bindings = new List<WorldMapFlowerPlacementController.PlacementVisualBinding>();
            Vector2 cell = LoadGridCellSize();
            Vector2 clusterCell = LoadClusterCellSize();

            foreach (var definition in GetFlowerDefinitions())
            {
                string singleKey = definition.Id + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Single;
                string clusterKey = definition.Id + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Cluster;
                var single = CreateSceneFlowerVisual(previewRoot.transform, uiLayer,
                    "PreviewVisual_" + SafeName(singleKey), definition.SingleSprite, cell, Vector2Int.one);
                var cluster = CreateSceneFlowerVisual(previewRoot.transform, uiLayer,
                    "PreviewVisual_" + SafeName(clusterKey), definition.ClusterSprite, clusterCell, Vector2Int.one);
                bindings.Add(new WorldMapFlowerPlacementController.PlacementVisualBinding());
                bindings.Add(new WorldMapFlowerPlacementController.PlacementVisualBinding());
            }

            return bindings;
        }

        private static List<WorldMapPlacementSlot> ConfigurePlacementSlots(Transform placementRoot, int uiLayer)
        {
            var result = new List<WorldMapPlacementSlot>(PlacementSlotCount);
            Vector2 cell = LoadGridCellSize();
            Vector2 clusterCell = LoadClusterCellSize();
            var definitions = GetFlowerDefinitions();

            for (int slotIndex = 0; slotIndex < PlacementSlotCount; slotIndex++)
            {
                var slotObject = EnsureChild(placementRoot, "PlacementSlot_" + slotIndex.ToString("00"), 0);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(slotObject);
                WorldMapPlacementSlot[] existingSlots = slotObject.GetComponents<WorldMapPlacementSlot>();
                WorldMapPlacementSlot? slot = null;
                for (int existingIndex = 0; existingIndex < existingSlots.Length; existingIndex++)
                {
                    if (existingSlots[existingIndex].HasVisualBindings)
                    {
                        slot = existingSlots[existingIndex];
                        break;
                    }
                }
                slot ??= existingSlots.Length > 0 ? existingSlots[0] : GetOrAdd<WorldMapPlacementSlot>(slotObject);
                for (int duplicateIndex = 0; duplicateIndex < existingSlots.Length; duplicateIndex++)
                {
                    if (existingSlots[duplicateIndex] != slot)
                        UnityEngine.Object.DestroyImmediate(existingSlots[duplicateIndex]);
                }
                var metadata = GetOrAdd<WorldMapPlacedFlower>(slotObject);
                var collider = GetOrAdd<BoxCollider2D>(slotObject);
                collider.isTrigger = true;
                collider.enabled = false;
                var visualRoot = slotObject.transform;

                RemoveChildrenWithPrefix(visualRoot, "PlacedVisual_");
                var bindings = new List<(string Key, GameObject Visual)>();
                foreach (var definition in definitions)
                {
                    string singleKey = definition.Id + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Single;
                    string clusterKey = definition.Id + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Cluster;
                    var single = CreateSceneFlowerVisual(visualRoot, uiLayer,
                        "PlacedVisual_" + SafeName(singleKey), definition.SingleSprite, cell, Vector2Int.one);
                    var cluster = CreateSceneFlowerVisual(visualRoot, uiLayer,
                        "PlacedVisual_" + SafeName(clusterKey), definition.ClusterSprite, clusterCell, Vector2Int.one);
                    bindings.Add((singleKey, single));
                    bindings.Add((clusterKey, cluster));
                }

                var slotSo = new SerializedObject(slot);
                // The scene can contain components from an older placement
                // prototype.  Always bind the current WorldMapPlacementSlot
                // instance explicitly, otherwise it may retain an empty
                // _visualRoot/_visuals and restore will be invisible.
                SetObject(slotSo, "_visualRoot", visualRoot);
                SetObject(slotSo, "_occupancyCollider", collider);
                SetObject(slotSo, "_metadata", metadata);
                var visualProperty = slotSo.FindProperty("_visuals")!;
                visualProperty.arraySize = bindings.Count;
                for (int i = 0; i < bindings.Count; i++)
                {
                    var element = visualProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_key")!.stringValue = bindings[i].Key;
                    element.FindPropertyRelative("_visual")!.objectReferenceValue = bindings[i].Visual;
                }
                slotSo.ApplyModifiedPropertiesWithoutUndo();
                slotObject.SetActive(false);
                result.Add(slot);
            }

            return result;
        }

        private static void ConfigurePreviewBindings(
            SerializedObject controller,
            List<WorldMapFlowerPlacementController.PlacementVisualBinding> bindings)
        {
            var property = controller.FindProperty("_previewVisuals")!;
            property.arraySize = bindings.Count;
            // PlacementVisualBinding 的字段通过下面的资源化列表重新构建，避免运行时创建视觉绑定。
            // 具体 GameObject 引用在 ConfigurePreviewBindingObjects 中写入。
            var root = GameObject.Find("WorldMapPlacedFlowers");
            var previewRoot = root != null ? root.transform.Find("FlowerPlacementPreview") : null;
            if (previewRoot == null) return;

            int index = 0;
            foreach (var definition in GetFlowerDefinitions())
            {
                foreach (var type in new[]
                         {
                             WorldMapFlowerPlacementController.PlacementVisualType.Single,
                             WorldMapFlowerPlacementController.PlacementVisualType.Cluster
                         })
                {
                    string key = definition.Id + "|" + type;
                    var element = property.GetArrayElementAtIndex(index++);
                    element.FindPropertyRelative("_key")!.stringValue = key;
                    var visual = previewRoot.Find("PreviewVisual_" + SafeName(key));
                    element.FindPropertyRelative("_visual")!.objectReferenceValue = visual != null
                        ? visual.gameObject
                        : null;
                }
            }
        }

        private static void ConfigurePlacementSlotReferences(
            SerializedObject controller,
            List<WorldMapPlacementSlot> slots)
        {
            var property = controller.FindProperty("_placementSlots")!;
            property.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                property.GetArrayElementAtIndex(i)!.objectReferenceValue = slots[i];
        }

        private static GameObject CreateSceneFlowerVisual(
            Transform parent,
            int layer,
            string name,
            Sprite? sprite,
            Vector2 cellSize,
            Vector2Int footprint)
        {
            var visual = EnsureChild(parent, name, layer);
            var renderer = GetOrAdd<SpriteRenderer>(visual);
            renderer.sprite = sprite;
            renderer.sortingLayerName = FlowerSortingLayerName;
            renderer.sortingOrder = 0;
            renderer.color = Color.white;
            visual.transform.localRotation = Quaternion.identity;
            Vector3 scale = CalculateSpriteScale(sprite, cellSize, footprint);
            visual.transform.localScale = scale;
            visual.transform.localPosition = sprite != null
                ? new Vector3(0f, -sprite.bounds.min.y * scale.y, 0f)
                : Vector3.zero;
            visual.SetActive(false);
            return visual;
        }

        private static Vector3 CalculateSpriteScale(Sprite? sprite, Vector2 cellSize, Vector2Int footprint)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
                return Vector3.one;

            Vector2 target = Vector2.Scale((Vector2)footprint, cellSize) * 0.82f;
            Vector2 source = sprite.bounds.size;
            float scale = Mathf.Min(target.x / source.x, target.y / source.y);
            return new Vector3(scale, scale, 1f);
        }

        private static GameObject EnsurePlacementGrid(Transform parent)
        {
            var grid = EnsureChild(parent, "FlowerPlacementGrid", 0);
            grid.transform.position = Vector3.zero;
            return grid;
        }

        private static void ConfigureGrid(GameObject grid, Collider2D surface, Material? material)
        {
            RemoveChildrenWithPrefix(grid.transform, "GridLine_");
            Bounds bounds = GetAuthoringBounds(surface);
            Vector2 cell = LoadGridCellSize();
            Vector2 origin = bounds.min;
            int verticalCount = Mathf.CeilToInt(bounds.size.x / cell.x) + 1;
            int horizontalCount = Mathf.CeilToInt(bounds.size.y / cell.y) + 1;

            for (int i = 0; i < verticalCount; i++)
            {
                float x = origin.x + i * cell.x;
                CreateGridLine(grid.transform, "GridLine_V_" + i.ToString("00"),
                    new Vector3(x, bounds.min.y, 0f), new Vector3(x, bounds.max.y, 0f), material);
            }

            for (int i = 0; i < horizontalCount; i++)
            {
                float y = origin.y + i * cell.y;
                CreateGridLine(grid.transform, "GridLine_H_" + i.ToString("00"),
                    new Vector3(bounds.min.x, y, 0f), new Vector3(bounds.max.x, y, 0f), material);
            }

            // 基线由 ConfigureFixedBaselineDefinitions 单独作者化为十一条固定节点。
            // 这里不能再遍历 BaselineItem 生成线，否则删除物体会错误地删除基线。
            grid.SetActive(false);
        }

        private static Bounds GetAuthoringBounds(Collider2D surface)
        {
            if (surface is BoxCollider2D box)
            {
                Vector3 worldCenter = box.transform.TransformPoint(box.offset);
                Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
                worldSize = new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), 0f);
                return new Bounds(worldCenter, worldSize);
            }

            return surface.bounds;
        }

        private static void CreateGridLine(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            Material? material)
        {
            var lineObject = EnsureChild(parent, name, 0);
            var line = GetOrAdd<LineRenderer>(lineObject);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.startColor = new Color(1f, 1f, 1f, 0.42f);
            line.endColor = new Color(1f, 1f, 1f, 0.42f);
            line.sortingLayerName = FlowerSortingLayerName;
            line.sortingOrder = 112;
            if (material != null) line.sharedMaterial = material;
        }

        private static GameObject EnsurePreviewRoot(Transform parent)
        {
            var root = EnsureChild(parent, "FlowerPlacementPreview", 0);
            root.transform.position = Vector3.zero;
            root.SetActive(false);
            return root;
        }

        private static BoxCollider2D EnsurePlacementBounds()
        {
            var boundsGo = GameObject.Find("FlowerPlacementBounds");
            bool createdObject = boundsGo == null;
            if (boundsGo == null)
            {
                boundsGo = new GameObject("FlowerPlacementBounds");
                boundsGo.transform.position = new Vector3(0f, -3f, 0f);
            }

            BoxCollider2D? existingBox = boundsGo.GetComponent<BoxCollider2D>();
            bool initializeShape = createdObject || existingBox == null;
            var box = existingBox ?? boundsGo.AddComponent<BoxCollider2D>();
            if (initializeShape || box.size.sqrMagnitude <= 0.0001f)
            {
                box.size = new Vector2(36f, 8.96f);
                box.offset = Vector2.zero;
            }
            box.isTrigger = true;
            box.enabled = false;
            return box;
        }

        private static List<WorldMapFlowerPlacementRegion> EnsurePlacementRegions(
            Transform parent,
            Collider2D fallbackSurface)
        {
            Bounds fallbackBounds = GetAuthoringBounds(fallbackSurface);
            float halfWidth = Mathf.Max(0.5f, fallbackBounds.size.x * 0.5f);
            var regions = new List<WorldMapFlowerPlacementRegion>(2)
            {
                EnsurePlacementRegion(parent, "FlowerPlacementRegion_Angel",
                    EmotionFlowerCatalog.OwnerAngel,
                    new Vector2(fallbackBounds.min.x + halfWidth * 0.5f, fallbackBounds.center.y),
                    new Vector2(halfWidth, fallbackBounds.size.y)),
                EnsurePlacementRegion(parent, "FlowerPlacementRegion_Demon",
                    EmotionFlowerCatalog.OwnerDemon,
                    new Vector2(fallbackBounds.min.x + halfWidth + halfWidth * 0.5f, fallbackBounds.center.y),
                    new Vector2(halfWidth, fallbackBounds.size.y))
            };

            return regions;
        }

        private static WorldMapFlowerPlacementRegion EnsurePlacementRegion(
            Transform parent,
            string name,
            string owner,
            Vector2 defaultCenter,
            Vector2 defaultSize)
        {
            Transform? existing = parent.Find(name);
            bool createdObject = existing == null;
            GameObject regionObject = existing != null
                ? existing.gameObject
                : EnsureChild(parent, name, 0);
            WorldMapFlowerPlacementRegion? existingRegion =
                regionObject.GetComponent<WorldMapFlowerPlacementRegion>();
            var region = existingRegion ?? GetOrAdd<WorldMapFlowerPlacementRegion>(regionObject);
            BoxCollider2D? existingBox = regionObject.GetComponent<BoxCollider2D>();
            var box = existingBox ?? regionObject.AddComponent<BoxCollider2D>();

            Vector3 serializedPosition = regionObject.transform.localPosition;
            if (regionObject.transform is RectTransform existingRectTransform)
                serializedPosition = existingRectTransform.anchoredPosition;
            bool isLegacyZeroOffset = existingRegion != null &&
                                      existingRegion.DefaultsInitialized &&
                                      box.offset.sqrMagnitude <= 0.0001f &&
                                      serializedPosition.sqrMagnitude > 0.0001f;
            bool initializeShape = createdObject || existingRegion == null ||
                                   !existingRegion.DefaultsInitialized || isLegacyZeroOffset;

            if (initializeShape || box.size.sqrMagnitude <= 0.0001f)
            {
                regionObject.transform.localPosition = Vector3.zero;
                if (regionObject.transform is RectTransform rectTransform)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.zero;
                    rectTransform.pivot = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
                regionObject.transform.rotation = Quaternion.identity;
                regionObject.transform.localScale = Vector3.one;
                box.size = defaultSize;
                Vector3 localCenter = regionObject.transform.InverseTransformPoint(
                    new Vector3(defaultCenter.x, defaultCenter.y, 0f));
                box.offset = new Vector2(localCenter.x, localCenter.y);
            }

            box.isTrigger = true;
            box.enabled = false;
            var serialized = new SerializedObject(region);
            serialized.FindProperty("_owner")!.stringValue = EmotionFlowerCatalog.NormalizeOwner(owner);
            serialized.FindProperty("_boundsCollider")!.objectReferenceValue = box;
            serialized.FindProperty("_defaultsInitialized")!.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(region);
            EditorUtility.SetDirty(box);
            return region;
        }

        private static void ConfigurePlacementRegions(
            SerializedObject controller,
            List<WorldMapFlowerPlacementRegion> regions)
        {
            var property = controller.FindProperty("_placementRegions")!;
            property.arraySize = regions.Count;
            for (int i = 0; i < regions.Count; i++)
                property.GetArrayElementAtIndex(i)!.objectReferenceValue = regions[i];
        }

        private static Material? EnsureGridMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GridMaterialPath);
            if (material != null) return material;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            material = new Material(shader)
            {
                name = "PlacementGrid",
                color = new Color(1f, 1f, 1f, 0.42f)
            };
            AssetDatabase.CreateAsset(material, GridMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private sealed class FlowerDefinition
        {
            public string Id = string.Empty;
            public Sprite? SingleSprite;
            public Sprite? ClusterSprite;
        }

        private static List<FlowerDefinition> GetFlowerDefinitions()
        {
            var definitions = new List<FlowerDefinition>(EmotionTypes.Length * 2);
            foreach (string emotion in EmotionTypes)
            {
                foreach (string owner in new[] { EmotionFlowerCatalog.OwnerAngel, EmotionFlowerCatalog.OwnerDemon })
                {
                    string ownerDisplay = EmotionFlowerCatalog.ResolveOwnerDisplayName(owner);
                    definitions.Add(new FlowerDefinition
                    {
                        Id = owner + "|" + emotion,
                        SingleSprite = LoadSprite(PlacementSingleArtDir + "/" +
                                                   ownerDisplay + "-" + emotion + ".PNG"),
                        ClusterSprite = LoadSprite(FlowerCodexArtDir + "/花丛/" +
                                                    ownerDisplay + "-" + emotion + "（花丛）.PNG")
                    });
                }
            }

            return definitions;
        }

        private static Vector2 LoadGridCellSize()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GridReferencePath);
            return sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f
                ? sprite.bounds.size
                : new Vector2(4.01f, 2.24f);
        }

        private static Vector2 LoadClusterCellSize()
        {
            var cluster = GameObject.Find("花丛 3");
            if (cluster == null)
            {
                foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (candidate.name == "花丛 3" && candidate.scene.IsValid())
                    {
                        cluster = candidate;
                        break;
                    }
                }
            }
            if (cluster == null)
                return new Vector2(3.99f, 2.22f);
            var collider = cluster != null ? cluster.GetComponent<BoxCollider2D>() : null;
            if (collider != null && collider.size.x > 0f && collider.size.y > 0f)
            {
                Vector3 scale = collider.transform.lossyScale;
                return Vector2.Scale(collider.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            }
            var renderer = cluster != null ? cluster.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && renderer.bounds.size.x > 0f && renderer.bounds.size.y > 0f)
                return renderer.bounds.size;
            return new Vector2(3.99f, 2.22f);
        }

        private sealed class FixedBaselineSpec
        {
            public readonly string Id;
            public readonly string LineName;
            public readonly int Slot;
            public readonly WorldMapBaselineDefinition.BaselineGroup Group;
            public readonly Color Color;
            public readonly float BaselineY;
            public readonly float XOffset;
            public readonly bool AllowFlowerPlacement;

            public FixedBaselineSpec(string id, string lineName, int slot,
                WorldMapBaselineDefinition.BaselineGroup group, Color color,
                float baselineY, float xOffset, bool allowFlowerPlacement)
            {
                Id = id;
                LineName = lineName;
                Slot = slot;
                Group = group;
                Color = color;
                BaselineY = baselineY;
                XOffset = xOffset;
                AllowFlowerPlacement = allowFlowerPlacement;
            }
        }

        private static readonly FixedBaselineSpec[] FixedBaselineSpecs =
        {
            new("Environment_Sky", "BaselineLine_天空", 0,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), 5.97f, 0f, false),
            new("Environment_Stars", "BaselineLine_星星", 1,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), 8.4f, 0f, false),
            new("Environment_Clouds", "BaselineLine_云", 2,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), 6.8f, 0f, false),
            new("Environment_Back", "BaselineLine_大树_后", 3,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), 2.3371658f, 0f, false),
            new("Environment_Front", "BaselineLine_大树_前", 4,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), 1.5571656f, 0f, false),
            new("Environment_Ground", "BaselineLine_地面", 5,
                WorldMapBaselineDefinition.BaselineGroup.Environment,
                new Color(0.55f, 0.88f, 1f, 0.95f), -3.6354f, 0f, false),
            new("Flower_Back", "BaselineLine_花丛 4", 6,
                WorldMapBaselineDefinition.BaselineGroup.Flower,
                Color.white, -2.5628343f, 2.005f, true),
            new("Flower_MidBack", "BaselineLine_花丛 1", 7,
                WorldMapBaselineDefinition.BaselineGroup.Flower,
                Color.white, -2.5728343f, 2.005f, true),
            new("Character", "BaselineLine_Pet_Angel", 8,
                WorldMapBaselineDefinition.BaselineGroup.Character,
                new Color(1f, 0.18f, 0.18f, 0.95f), -3.752758f, 0f, false),
            new("Flower_MidFront", "BaselineLine_花丛 2", 9,
                WorldMapBaselineDefinition.BaselineGroup.Flower,
                Color.white, -3.7528343f, 0f, true),
            new("Flower_Front", "BaselineLine_花丛 3", 10,
                WorldMapBaselineDefinition.BaselineGroup.Flower,
                Color.white, -4.3128343f, 2.005f, true)
        };

        private static void ConfigureFixedBaselineDefinitions(
            GameObject grid, Collider2D surface, Material? material)
        {
            if (grid == null || grid.name != "FlowerPlacementGrid" ||
                grid.transform.parent == null || grid.transform.parent.name != "WorldMapPlacedFlowers")
                return;

            Bounds bounds = GetAuthoringBounds(surface);
            var retained = new HashSet<string>();
            for (int i = 0; i < FixedBaselineSpecs.Length; i++)
            {
                FixedBaselineSpec spec = FixedBaselineSpecs[i];
                retained.Add(spec.LineName);
                GameObject lineObject = EnsureChild(grid.transform, spec.LineName, 0);
                LineRenderer line = GetOrAdd<LineRenderer>(lineObject);
                WorldMapBaselineDefinition definition = GetOrAdd<WorldMapBaselineDefinition>(lineObject);
                bool firstAuthoring = string.IsNullOrEmpty(definition.Id);
                line.useWorldSpace = true;
                line.positionCount = 2;
                if (firstAuthoring)
                {
                    line.SetPosition(0, new Vector3(bounds.min.x + spec.XOffset, spec.BaselineY, 0f));
                    line.SetPosition(1, new Vector3(bounds.max.x + spec.XOffset, spec.BaselineY, 0f));
                }
                line.startWidth = 0.025f;
                line.endWidth = 0.025f;
                line.startColor = spec.Color;
                line.endColor = spec.Color;
                line.sortingLayerName = FlowerSortingLayerName;
                line.sortingOrder = 112;
                if (material != null) line.sharedMaterial = material;

                float baselineY = line.GetPosition(0).y;
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_id")!.stringValue = spec.Id;
                serialized.FindProperty("_slotIndex")!.intValue = spec.Slot;
                SerializedProperty renderOrder = serialized.FindProperty("_renderOrder")!;
                if (renderOrder.intValue < 0)
                    renderOrder.intValue = spec.Slot;
                serialized.FindProperty("_group")!.enumValueIndex = (int)spec.Group;
                serialized.FindProperty("_editorColor")!.colorValue = spec.Color;
                serialized.FindProperty("_displayName")!.stringValue = spec.Id;
                serialized.FindProperty("_allowFlowerPlacement")!.boolValue = spec.AllowFlowerPlacement;
                serialized.FindProperty("_xOffset")!.floatValue = spec.XOffset;
                serialized.FindProperty("_minX")!.floatValue = bounds.min.x;
                serialized.FindProperty("_maxX")!.floatValue = bounds.max.x;
                serialized.FindProperty("_baselineY")!.floatValue = baselineY;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);

                BindBaselineItems(definition, spec);
            }

            // 只清理 FlowerPlacementGrid 下旧的 BaselineLine 节点，绝不触碰场景根对象。
            for (int i = grid.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = grid.transform.GetChild(i);
                if (child.name.StartsWith("BaselineLine_", StringComparison.Ordinal) &&
                    !retained.Contains(child.name))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void BindBaselineItems(
            WorldMapBaselineDefinition definition, FixedBaselineSpec spec)
        {
            BaselineItem[] items = UnityEngine.Object.FindObjectsByType<BaselineItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                BaselineItem item = items[i];
                if (!item.gameObject.scene.IsValid() || item.gameObject.scene.path != ScenePath)
                    continue;
                if (ResolveBaselineSlot(item.name) != spec.Slot) continue;

                SerializedObject serialized = new SerializedObject(item);
                serialized.FindProperty("_baselineDefinition")!.objectReferenceValue = definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                item.RefreshBaselineBinding();
                EditorUtility.SetDirty(item);
            }
        }

        private static int ResolveBaselineSlot(string objectName)
        {
            string normalized = objectName.Replace(" ", string.Empty).Replace("_", string.Empty);
            if (normalized == "天空") return 0;
            if (normalized == "大树3" || normalized == "大树4" || normalized == "大树5") return 3;
            if (normalized == "大树1" || normalized == "大树2" || normalized == "邮箱" || normalized == "桥" || normalized == "室内") return 4;
            if (normalized == "地面") return 5;
            if (normalized == "花丛4") return 6;
            if (normalized == "花丛1") return 7;
            if (normalized == "PetAngel" || normalized == "PetDevil" || normalized == "天使1" || normalized == "天使2" || normalized == "天使3") return 8;
            if (normalized == "花丛2") return 9;
            if (normalized == "花丛3") return 10;
            return -1;
        }

        private static void ConfigurePlacementLayers(SerializedObject controller, Collider2D surface)
        {
            GameObject? grid = GameObject.Find("FlowerPlacementGrid");
            if (grid == null) return;

            var definitions = new List<WorldMapBaselineDefinition>();
            foreach (WorldMapBaselineDefinition definition in
                     grid.GetComponentsInChildren<WorldMapBaselineDefinition>(true))
            {
                if (definition.Group == WorldMapBaselineDefinition.BaselineGroup.Flower &&
                    definition.AllowFlowerPlacement)
                    definitions.Add(definition);
            }
            definitions.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));

            SerializedProperty property = controller.FindProperty("_placementLayers")!;
            property.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
            {
                WorldMapBaselineDefinition definition = definitions[i];
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_sourceBaselineDefinition")!.objectReferenceValue = definition;
            }
        }

        private static void EnsureWorldMapPetBaselines()
        {
            WorldMapBaselineDefinition? characterDefinition = null;
            GameObject? grid = GameObject.Find("FlowerPlacementGrid");
            if (grid != null)
            {
                foreach (WorldMapBaselineDefinition candidate in
                         grid.GetComponentsInChildren<WorldMapBaselineDefinition>(true))
                {
                    if (candidate.Group == WorldMapBaselineDefinition.BaselineGroup.Character)
                    {
                        characterDefinition = candidate;
                        break;
                    }
                }
            }

            foreach (string petName in new[] { "Pet_Angel", "Pet_Devil" })
            {
                GameObject? pet = GameObject.Find(petName);
                if (pet == null) continue;

                BaselineItem baseline = GetOrAdd<BaselineItem>(pet);
                var serialized = new SerializedObject(baseline);
                serialized.FindProperty("_baselineDefinition")!.objectReferenceValue = characterDefinition;
                serialized.FindProperty("_allowDrag")!.boolValue = false;
                serialized.FindProperty("_solidCollider")!.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                baseline.RefreshSortingOrder();
                EditorUtility.SetDirty(baseline);
            }
        }

        private static Sprite? LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string SafeName(string value)
        {
            return value.Replace("|", "_").Replace("·", "_").Replace(" ", "_");
        }

        private static TMP_Text EnsureTextChild(
            Transform parent,
            string name,
            int layer,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            float fontSize)
        {
            var go = EnsureChild(parent, name, layer);
            ConfigureRect(go, anchorMin, anchorMax, position, size, new Vector2(0.5f, 0.5f));
            var tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.34f, 0.20f, 0.10f, 1f);
            return tmp;
        }

        private static Image EnsureImageChild(
            Transform parent,
            string name,
            int layer,
            Sprite? sprite,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            var go = EnsureChild(parent, name, layer);
            ConfigureRect(go, anchor, anchor, position, size, anchor);
            var image = GetOrAdd<Image>(go);
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            return image;
        }

        private static Button EnsureButton(
            Transform parent,
            int layer,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var go = EnsureChild(parent, name, layer);
            ConfigureRect(go, anchorMin, anchorMax, position, size, new Vector2(0.5f, 0.5f));
            var image = GetOrAdd<Image>(go);
            image.color = color;
            var button = GetOrAdd<Button>(go);
            button.targetGraphic = image;
            var text = EnsureTextChild(go.transform, "Label", layer, label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18f);
            text.color = Color.white;
            return button;
        }

        private static Button EnsureAnchoredButton(
            Transform parent,
            int layer,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot,
            Color color)
        {
            var button = EnsureButton(parent, layer, name, label, position, size, anchor, anchor, color);
            button.GetComponent<RectTransform>()!.pivot = pivot;
            return button;
        }

        private static GameObject EnsureChild(Transform parent, string name, int layer)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            GetOrAdd<RectTransform>(go);
            return go;
        }

        private static void ConfigureRect(
            GameObject go,
            Vector2 min,
            Vector2 max,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
                UnityEngine.Object.DestroyImmediate(component);
        }

        private static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object? value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetObject(SerializedProperty parent, string propertyName, UnityEngine.Object? value)
        {
            var property = parent.FindPropertyRelative(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void RemoveChildrenWithPrefix(Transform parent, string prefix)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                if (parent.GetChild(i).name.StartsWith(prefix, StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}
#endif
