#nullable enable
#if UNITY_EDITOR
using System;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the WorldMap wish UI. Art, layout, list rows and star slots are saved in the scene.
    /// Runtime only toggles authored objects and fills record text.
    /// </summary>
    public static class WorldMapWishSystemAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string PanelName = "WorldMapWishSystemPanel";
        private const string WishArtRoot = "Assets/_Project/Art/WorldMap/\u8BB8\u613F\u6811/";
        private const string WishArtItemRoot = WishArtRoot + "item/";
        private static readonly string[] StarArtPaths =
        {
            WishArtRoot + "star.png", WishArtRoot + "star1.png", WishArtRoot + "star2.png",
            WishArtRoot + "star3.png", WishArtRoot + "star4.png", WishArtRoot + "star5.png",
            WishArtRoot + "star6.png", WishArtRoot + "star7.png", WishArtRoot + "star8.png",
            WishArtRoot + "star9.png", WishArtRoot + "star10.png"
        };

        // These coordinates are inside the illustrated tree at the upper-left of background.png.
        private static readonly Vector2[] PanelTreeStarPositions =
        {
            new(380f, 710f), new(535f, 735f), new(690f, 750f), new(845f, 715f),
            new(300f, 585f), new(470f, 605f), new(650f, 620f), new(835f, 595f),
            new(425f, 465f), new(575f, 495f), new(730f, 505f), new(900f, 465f)
        };

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Wish System")]
        public static void Patch()
        {
            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject? canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[WorldMapWishSystemAuthoring] WorldMap Canvas not found.");
                return;
            }

            int uiLayer = canvas.layer;
            GameObject panel = EnsureChild(canvas.transform, PanelName, uiLayer);
            ConfigurePanelRoot(panel);
            GameObject window = EnsureRectChild(panel.transform, "Window", uiLayer, new Vector2(1920f, 1080f), Vector2.zero);
            ConfigureFullscreenRect(window);
            ConfigureImage(window, new Color(1f, 1f, 1f, 0f), false);

            GameObject main = EnsureRectChild(window.transform, "MainView", uiLayer, new Vector2(1920f, 1080f), Vector2.zero);
            GameObject input = EnsureRectChild(window.transform, "InputView", uiLayer, new Vector2(1920f, 1080f), Vector2.zero);
            GameObject detail = EnsureRectChild(window.transform, "DetailView", uiLayer, new Vector2(1920f, 1080f), Vector2.zero);
            GameObject memory = EnsureRectChild(window.transform, "MemoryListView", uiLayer, new Vector2(1920f, 1080f), Vector2.zero);
            ConfigureFullscreenRect(main);
            ConfigureFullscreenRect(input);
            ConfigureFullscreenRect(detail);
            ConfigureFullscreenRect(memory);

            EnsureImageChild(main.transform, "WishArt_MainBackground", WishArtRoot + "background.png", uiLayer, true).transform.SetAsFirstSibling();
            EnsureImageChild(input.transform, "WishArt_InputBackground", WishArtRoot + "background.png", uiLayer, true).transform.SetAsFirstSibling();
            EnsureImageChild(detail.transform, "WishArt_DetailBackground", WishArtItemRoot + "background.png", uiLayer, true).transform.SetAsFirstSibling();
            EnsureImageChild(memory.transform, "WishArt_MemoryBackground", WishArtItemRoot + "background.png", uiLayer, true).transform.SetAsFirstSibling();

            TMP_Text title = EnsureText(window.transform, "Title", uiLayer, string.Empty, 34,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(0f, 48f));
            title.gameObject.SetActive(false);

            Button close = EnsureButton(window.transform, "Btn_Close", uiLayer, string.Empty,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -74f), new Vector2(103f, 105f));
            ApplyButtonArt(close, WishArtRoot + "close.png");
            Button wish = EnsureButton(main.transform, "Btn_Wish", uiLayer, string.Empty,
                Vector2.zero, Vector2.zero, new Vector2(1430f, 260f), new Vector2(214f, 62f));
            ApplyButtonArt(wish, WishArtRoot + "wish.png");
            Button all = EnsureButton(main.transform, "Btn_All", uiLayer, string.Empty,
                Vector2.zero, Vector2.zero, new Vector2(1690f, 870f), new Vector2(99f, 102f));
            ApplyButtonArt(all, WishArtRoot + "item_button.png");

            Button[] panelStars = new Button[WorldMapWishService.SlotCount];
            for (int index = 0; index < panelStars.Length; index++)
            {
                Vector2 position = PanelTreeStarPositions[index % PanelTreeStarPositions.Length];
                panelStars[index] = EnsureButton(main.transform, $"WishStarSlot_{index:00}", uiLayer, string.Empty,
                    Vector2.zero, Vector2.zero, position, new Vector2(84f, 84f));
                ApplyButtonArt(panelStars[index], StarArtPaths[index % StarArtPaths.Length]);
                panelStars[index].gameObject.SetActive(false);
            }

            TMP_InputField inputField = EnsureInputField(input.transform, uiLayer, "WishInputField");
            ApplyRect(inputField.gameObject, Vector2.zero, Vector2.zero, new Vector2(1310f, 400f), new Vector2(576f, 188f));
            ApplyInputArt(inputField, WishArtRoot + "input.png");
            Image inputPromptImage = inputField.GetComponent<Image>()!;
            GameObject inputFocusedVisual = EnsureImageChild(input.transform, "WishInputFocusedVisual",
                "Assets/_Project/Art/WorldMap/UI输入框去字/wish_input.png", uiLayer, false);
            ApplyRect(inputFocusedVisual, Vector2.zero, Vector2.zero, new Vector2(1310f, 400f), new Vector2(576f, 188f));
            Image inputFocusedImage = inputFocusedVisual.GetComponent<Image>()!;
            inputFocusedImage.raycastTarget = false;
            inputFocusedVisual.SetActive(false);
            PlaceBehind(inputFocusedVisual.transform, inputField.transform);
            Button submit = EnsureButton(input.transform, "Btn_SubmitWish", uiLayer, string.Empty,
                Vector2.zero, Vector2.zero, new Vector2(1310f, 200f), new Vector2(214f, 62f));
            ApplyButtonArt(submit, WishArtRoot + "wish.png");
            Button cancelInput = EnsureButton(input.transform, "Btn_CancelInput", uiLayer, string.Empty,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -74f), new Vector2(103f, 105f));
            ApplyButtonArt(cancelInput, WishArtRoot + "close.png");

            GameObject detailContentArt = EnsureImageChild(detail.transform, "WishArt_DetailContent", WishArtItemRoot + "content.png", uiLayer, false);
            ApplyRect(detailContentArt, Vector2.zero, Vector2.zero, new Vector2(450f, 625f), new Vector2(556f, 259f));
            TMP_Text detailContent = EnsureText(detail.transform, "DetailContent", uiLayer, string.Empty, 30,
                Vector2.zero, Vector2.zero, new Vector2(450f, 625f), new Vector2(490f, 175f));
            detailContent.raycastTarget = false;
            PlaceBehind(detailContentArt.transform, detailContent.transform);

            TMP_Text detailCreated = EnsureText(detail.transform, "DetailCreated", uiLayer, string.Empty, 22,
                Vector2.zero, Vector2.zero, new Vector2(450f, 440f), new Vector2(490f, 60f));
            TMP_Text detailState = EnsureText(detail.transform, "DetailState", uiLayer, string.Empty, 22,
                Vector2.zero, Vector2.zero, new Vector2(450f, 380f), new Vector2(490f, 50f));
            TMP_Text detailFulfilled = EnsureText(detail.transform, "DetailFulfilled", uiLayer, string.Empty, 22,
                Vector2.zero, Vector2.zero, new Vector2(450f, 320f), new Vector2(490f, 50f));
            detailCreated.raycastTarget = false;
            detailState.raycastTarget = false;
            detailFulfilled.raycastTarget = false;

            GameObject startDateArt = EnsureImageChild(detail.transform, "WishArt_DetailStartDate", WishArtItemRoot + "start_date.png", uiLayer, false);
            ApplyRect(startDateArt, Vector2.zero, Vector2.zero, new Vector2(450f, 440f), new Vector2(554f, 100f));
            PlaceBehind(startDateArt.transform, detailCreated.transform);
            GameObject endDateArt = EnsureImageChild(detail.transform, "WishArt_DetailEndDate", WishArtItemRoot + "end_date.png", uiLayer, false);
            ApplyRect(endDateArt, Vector2.zero, Vector2.zero, new Vector2(450f, 320f), new Vector2(553f, 98f));
            PlaceBehind(endDateArt.transform, detailFulfilled.transform);

            Button fulfill = EnsureButton(detail.transform, "Btn_Fulfill", uiLayer, string.Empty,
                Vector2.zero, Vector2.zero, new Vector2(450f, 150f), new Vector2(252f, 86f));
            ApplyButtonArt(fulfill, WishArtItemRoot + "realize.png", WishArtItemRoot + "realize_grey.png");
            Button delete = EnsureButton(detail.transform, "Btn_Delete", uiLayer, string.Empty,
                Vector2.zero, Vector2.zero, new Vector2(690f, 150f), new Vector2(150f, 64f));
            Button detailClose = EnsureButton(detail.transform, "Btn_DetailClose", uiLayer, string.Empty,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -74f), new Vector2(103f, 105f));
            ApplyButtonArt(detailClose, WishArtItemRoot + "close.png");

            ScrollRect memoryScroll = EnsureWishListScrollRect(detail.transform, uiLayer);
            RectTransform memoryContent = memoryScroll.content!;
            TMP_Text[] entries = new TMP_Text[WorldMapWishService.SlotCount];
            Button[] entryButtons = new Button[WorldMapWishService.SlotCount];
            GameObject[] selectedVisuals = new GameObject[WorldMapWishService.SlotCount];
            for (int index = 0; index < entries.Length; index++)
            {
                Vector2 position = new Vector2(540f, -110f - index * 220f);
                entries[index] = EnsureText(memoryContent, $"MemoryEntry_{index:00}", uiLayer, string.Empty, 22,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(960f, 200f));
                entries[index].alignment = TextAlignmentOptions.Center;
                entries[index].raycastTarget = false;
                entryButtons[index] = EnsureImageOnText(entries[index], WishArtItemRoot + "item.png", WishArtItemRoot + "item_selected.png", uiLayer, out selectedVisuals[index]);
            }

            Button memoryClose = EnsureButton(memory.transform, "Btn_MemoryClose", uiLayer, string.Empty,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -74f), new Vector2(103f, 105f));
            ApplyButtonArt(memoryClose, WishArtItemRoot + "close.png");
            TMP_Text dialogue = EnsureText(window.transform, "Dialogue", uiLayer, string.Empty, 20,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(760f, 44f));
            dialogue.raycastTarget = false;

            WorldMapWishSystemController controller = GetOrAdd<WorldMapWishSystemController>(panel);
            SerializedObject controllerSo = new(controller);
            SetObject(controllerSo, "_panelRoot", panel);
            SetObject(controllerSo, "_mainView", main);
            SetObject(controllerSo, "_inputView", input);
            SetObject(controllerSo, "_detailView", detail);
            SetObject(controllerSo, "_memoryListView", memory);
            SetObject(controllerSo, "_wishButton", wish);
            SetObject(controllerSo, "_allButton", all);
            SetObject(controllerSo, "_closeButton", close);
            SetObject(controllerSo, "_submitButton", submit);
            SetObject(controllerSo, "_cancelInputButton", cancelInput);
            SetObject(controllerSo, "_detailCloseButton", detailClose);
            SetObject(controllerSo, "_memoryCloseButton", memoryClose);
            SetObject(controllerSo, "_fulfillButton", fulfill);
            SetObject(controllerSo, "_deleteButton", delete);
            SetObject(controllerSo, "_inputField", inputField);
            SetObject(controllerSo, "_inputPromptImage", inputPromptImage);
            SetObject(controllerSo, "_inputFocusedImage", inputFocusedImage);
            SetObject(controllerSo, "_dialogueText", dialogue);
            SetObject(controllerSo, "_detailContentText", detailContent);
            SetObject(controllerSo, "_detailCreatedText", detailCreated);
            SetObject(controllerSo, "_detailStateText", detailState);
            SetObject(controllerSo, "_detailFulfilledText", detailFulfilled);
            AssignObjectArray(controllerSo.FindProperty("_memoryEntryTexts"), entries);
            AssignObjectArray(controllerSo.FindProperty("_memoryEntryButtons"), entryButtons);
            AssignObjectArray(controllerSo.FindProperty("_memoryEntrySelectedVisuals"), selectedVisuals);
            SetObject(controllerSo, "_memoryScrollRect", memoryScroll);
            AssignObjectArray(controllerSo.FindProperty("_starSlots"), panelStars);
            AssignObjectArray(controllerSo.FindProperty("_worldStarSlots"), Array.Empty<Button>());
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            // Keep the scene tree as the clickable entry point, but do not render world-space wish stars.
            GameObject? wishingTree = FindSceneObject("\u8BB8\u613F\u6811");
            if (wishingTree != null)
            {
                Transform? legacySlots = wishingTree.transform.Find("WorldMapWishStarSlots");
                if (legacySlots != null) legacySlots.gameObject.SetActive(false);
                WorldMapWishTreeInteractable interactable = GetOrAdd<WorldMapWishTreeInteractable>(wishingTree);
                SerializedObject interactableSo = new(interactable);
                SetObject(interactableSo, "_wishSystem", controller);
                interactableSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(interactable);
            }

            main.SetActive(true);
            input.SetActive(false);
            detail.SetActive(false);
            memory.SetActive(false);
            panel.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldMapWishSystemAuthoring] Wish system authored. tree={(wishingTree != null)}, panelSlots={panelStars.Length}");
        }

        /// <summary>
        /// Adds only the focused no-placeholder art to the existing wish input.
        /// It intentionally does not rebuild the wish panel or its list slots.
        /// </summary>
        public static void PatchInputTaskMinimized()
        {
            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject? panel = GameObject.Find(PanelName);
            if (panel == null) return;
            Transform? inputView = panel.transform.Find("Window/InputView");
            TMP_InputField? inputField = inputView?.Find("WishInputField")?.GetComponent<TMP_InputField>();
            WorldMapWishSystemController? controller = panel.GetComponent<WorldMapWishSystemController>();
            if (inputView == null || inputField == null || controller == null) return;

            int uiLayer = panel.layer;
            GameObject focusedVisual = EnsureImageChild(inputView, "WishInputFocusedVisual",
                "Assets/_Project/Art/WorldMap/UI输入框去字/wish_input.png", uiLayer, false);
            ApplyRect(focusedVisual, Vector2.zero, Vector2.zero, new Vector2(1310f, 400f), new Vector2(576f, 188f));
            Image focusedImage = focusedVisual.GetComponent<Image>()!;
            focusedImage.raycastTarget = false;
            focusedVisual.SetActive(false);
            PlaceBehind(focusedVisual.transform, inputField.transform);

            SerializedObject controllerSo = new(controller);
            SetObject(controllerSo, "_inputField", inputField);
            SetObject(controllerSo, "_inputPromptImage", inputField.GetComponent<Image>());
            SetObject(controllerSo, "_inputFocusedImage", focusedImage);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapWishSystemAuthoring] Minimal wish input visual authored");
        }

        private static void ConfigurePanelRoot(GameObject panel)
        {
            RectTransform rt = GetOrAdd<RectTransform>(panel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            ConfigureImage(panel, new Color(0f, 0f, 0f, 0.55f), true);
            Canvas canvas = GetOrAdd<Canvas>(panel);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;
            GetOrAdd<GraphicRaycaster>(panel);
        }

        private static ScrollRect EnsureWishListScrollRect(Transform parent, int layer)
        {
            GameObject scrollGo = EnsureChild(parent, "WishMemoryScrollRect", layer);
            // The handbook list occupies only the right-hand card column.
            ApplyRect(scrollGo, Vector2.zero, Vector2.zero, new Vector2(1300f, 600f), new Vector2(1200f, 700f));
            ScrollRect scroll = GetOrAdd<ScrollRect>(scrollGo);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 55f;

            GameObject viewport = EnsureChild(scrollGo.transform, "Viewport", layer);
            ApplyRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ConfigureImage(viewport, new Color(1f, 1f, 1f, 0f), true);
            GetOrAdd<RectMask2D>(viewport);
            GameObject content = EnsureChild(viewport.transform, "Content", layer);
            RectTransform contentRt = GetOrAdd<RectTransform>(content);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(1080f, WorldMapWishService.SlotCount * 220f);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            return scroll;
        }

        private static Button EnsureImageOnText(TMP_Text text, string normalPath, string selectedPath, int layer, out GameObject selectedVisual)
        {
            Transform parent = text.transform.parent!;
            GameObject background = EnsureChild(parent, $"WishArt_{text.gameObject.name}_Background", layer);
            Image image = ConfigureImage(background, Color.white, true);
            Sprite? normal = AssetDatabase.LoadAssetAtPath<Sprite>(normalPath);
            if (normal != null)
            {
                image.sprite = normal;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            RectTransform source = text.rectTransform;
            RectTransform target = GetOrAdd<RectTransform>(background);
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localScale = source.localScale;
            PlaceBehind(background.transform, text.transform);
            Button button = GetOrAdd<Button>(background);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            Sprite? selected = AssetDatabase.LoadAssetAtPath<Sprite>(selectedPath);
            SpriteState state = button.spriteState;
            state.highlightedSprite = selected;
            state.pressedSprite = selected;
            state.selectedSprite = selected;
            button.spriteState = state;

            selectedVisual = EnsureChild(parent, $"WishArt_{text.gameObject.name}_Selected", layer);
            Image selectedImage = ConfigureImage(selectedVisual, Color.white, false);
            selectedImage.sprite = selected;
            selectedImage.type = Image.Type.Simple;
            selectedImage.preserveAspect = true;
            RectTransform selectedRt = GetOrAdd<RectTransform>(selectedVisual);
            selectedRt.anchorMin = source.anchorMin;
            selectedRt.anchorMax = source.anchorMax;
            selectedRt.pivot = source.pivot;
            selectedRt.anchoredPosition = source.anchoredPosition;
            selectedRt.sizeDelta = source.sizeDelta;
            selectedRt.localScale = source.localScale;
            PlaceBefore(selectedVisual.transform, text.transform);
            selectedVisual.SetActive(false);
            return button;
        }

        private static void PlaceBehind(Transform art, Transform foreground)
        {
            if (art.GetSiblingIndex() > foreground.GetSiblingIndex())
                art.SetSiblingIndex(foreground.GetSiblingIndex());
        }

        private static void PlaceBefore(Transform visual, Transform foreground)
        {
            int foregroundIndex = foreground.GetSiblingIndex();
            int targetIndex = visual.GetSiblingIndex() > foregroundIndex
                ? foregroundIndex
                : Mathf.Max(0, foregroundIndex - 1);
            if (visual.GetSiblingIndex() != targetIndex)
                visual.SetSiblingIndex(targetIndex);
        }

        private static TMP_InputField EnsureInputField(Transform parent, int layer, string name)
        {
            GameObject go = EnsureChild(parent, name, layer);
            Image image = ConfigureImage(go, Color.white, true);
            TMP_InputField field = GetOrAdd<TMP_InputField>(go);
            field.targetGraphic = image;
            field.characterLimit = 50;
            TextMeshProUGUI text = EnsureTextChild(go.transform, "Text", layer, string.Empty, 26);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = new Color(0.25f, 0.18f, 0.12f, 1f);
            text.raycastTarget = false;
            TextMeshProUGUI placeholder = EnsureTextChild(go.transform, "Placeholder", layer, string.Empty, 26);
            placeholder.alignment = TextAlignmentOptions.TopLeft;
            placeholder.color = new Color(0.45f, 0.4f, 0.35f, 0.75f);
            placeholder.raycastTarget = false;
            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }

        private static Button EnsureButton(Transform parent, string name, int layer, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name, layer);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            Image image = ConfigureImage(go, Color.white, true);
            Button button = GetOrAdd<Button>(go);
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label)) ConfigureButtonLabel(go.transform, layer, label);
            else HideButtonLabel(go.transform);
            return button;
        }

        private static TMP_Text EnsureText(Transform parent, string name, int layer, string value, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name, layer);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = value; tmp.fontSize = fontSize; tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.25f, 0.18f, 0.12f, 1f);
            tmp.raycastTarget = false;
            GetOrAdd<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>(go);
            return tmp;
        }

        private static TextMeshProUGUI EnsureTextChild(Transform parent, string name, int layer, string value, float fontSize)
        {
            GameObject go = EnsureChild(parent, name, layer);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(12f, 8f); rt.offsetMax = new Vector2(-12f, -8f);
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = value; tmp.fontSize = fontSize; tmp.raycastTarget = false;
            GetOrAdd<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>(go);
            return tmp;
        }

        private static void ApplyButtonArt(Button button, string normalPath, string? disabledPath = null)
        {
            Image image = GetOrAdd<Image>(button.gameObject);
            Sprite? normal = AssetDatabase.LoadAssetAtPath<Sprite>(normalPath);
            if (normal != null)
            {
                image.sprite = normal;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = normal;
            state.pressedSprite = normal;
            state.selectedSprite = normal;
            if (!string.IsNullOrWhiteSpace(disabledPath)) state.disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(disabledPath);
            button.spriteState = state;
            HideButtonLabel(button.transform);
        }

        private static void ApplyInputArt(TMP_InputField field, string path)
        {
            Image image = ConfigureImage(field.gameObject, Color.white, true);
            Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            field.targetGraphic = image;
            if (field.placeholder != null) field.placeholder.gameObject.SetActive(false);
        }

        private static GameObject EnsureImageChild(Transform parent, string name, string spritePath, int layer, bool fullscreen)
        {
            GameObject go = EnsureChild(parent, name, layer);
            Image image = ConfigureImage(go, Color.white, false);
            Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = !fullscreen;
            }

            RectTransform rt = GetOrAdd<RectTransform>(go);
            if (fullscreen) ConfigureFullscreenRect(go);
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            return go;
        }

        private static void ConfigureFullscreenRect(GameObject go)
        {
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        }

        private static GameObject EnsureRectChild(Transform parent, string name, int layer, Vector2 size, Vector2 position)
        {
            GameObject go = EnsureChild(parent, name, layer);
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
            return go;
        }

        private static void ConfigureButtonLabel(Transform parent, int layer, string value)
        {
            TextMeshProUGUI label = EnsureTextChild(parent, "Label", layer, value, 26);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
        }

        private static void HideButtonLabel(Transform parent)
        {
            Transform? label = parent.Find("Label");
            if (label != null) label.gameObject.SetActive(false);
        }

        private static Image ConfigureImage(GameObject go, Color color, bool raycast)
        {
            Image image = GetOrAdd<Image>(go);
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void ApplyRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty? property = serializedObject.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void AssignObjectArray(SerializedProperty? property, UnityEngine.Object[] values)
        {
            if (property == null) return;
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static GameObject EnsureChild(Transform parent, string name, int layer)
        {
            Transform? existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            return go;
        }

        private static GameObject? FindSceneObject(string objectName)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate.name == objectName && candidate.gameObject.scene.IsValid()) return candidate.gameObject;
            }

            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T? component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
#endif
