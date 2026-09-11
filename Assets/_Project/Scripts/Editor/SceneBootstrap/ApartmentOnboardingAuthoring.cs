#nullable enable
#if UNITY_EDITOR
using System;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>Explicit, additive onboarding authoring. Existing authored panels and Inspector layout are preserved.</summary>
    public static class ApartmentOnboardingAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string Art = "Assets/_Project/Art/";
        private const string PanelPath = Art + "UI/ApartmentOnboarding/Panel.png";
        private static readonly Color Ink = new(0.12f, 0.055f, 0.025f);

        [MenuItem("Tools/Gemini-Lab/Apartment/Author Onboarding")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先停止 Play。");
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath ? EditorSceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath);
            var viewport = UnityEngine.Object.FindFirstObjectByType<ApartmentViewportInputBridge>(FindObjectsInactive.Include);
            if (viewport == null) throw new InvalidOperationException("Apartment viewport missing.");
            var canvas = viewport.GetComponentInParent<Canvas>(true);
            if (canvas == null) throw new InvalidOperationException("Apartment canvas missing.");
            if (canvas.transform.Find("ApartmentOnboarding") != null)
            {
                Debug.Log("[ApartmentOnboarding] 已存在，保留场景中的文案、绑定与布局。请直接在 Inspector 编辑。");
                return;
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(PanelPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(180, 180, 180, 180);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            ApartmentFurnitureSelectionAuthoring.Patch();

            Load(Art+"新手引导/left.png");
            Load(Art+"新手引导/right.png");
            var root = Rect(canvas.transform, "ApartmentOnboarding", Vector2.zero, Vector2.zero);
            Stretch(root);
            var overlay = root.gameObject.AddComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 1000;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var controller = root.gameObject.AddComponent<ApartmentTutorialController>();
            var open = Button(root, "Btn_ApartmentTutorialOpen", "新手引导", new Vector2(-130, 54), new Vector2(180, 62));
            open.GetComponent<RectTransform>().anchorMin = open.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            var panel = Rect(root, "Panel_ApartmentTutorial", Vector2.zero, Vector2.zero);
            Stretch(panel);
            var shade = panel.gameObject.AddComponent<Image>();
            shade.color = new Color(0.12f, 0.07f, 0.10f, 0.72f);
            shade.raycastTarget = true;
            var card = Rect(panel, "TutorialCard", Vector2.zero, new Vector2(1320, 850));
            Image(card, Load(PanelPath));
            var pages = new GameObject[6];
            string[] titles = { "欢迎来到 ta 们的住所", "偷偷观察一下", "想亲自去看看？", "把鼠标放在家具上", "去隔壁串个门吧", "房间也会悄悄改变" };
            string[] bodies = {
                "这里是天使和恶魔的小窝。\n就算你什么都不做，ta 们也会按照自己的状态生活。\n偶尔来看看，也许会发现一些有意思的事情。",
                "留意下方的心情与精力。\n发呆、看书、画画、休息……不同状态会有不同表现。\n不一定要打扰他们，看看也很好。",
                "先点击一只桌宠，再用 WASD 或方向键移动。\n点击空地可以放开控制，让 ta 继续自由活动。\n靠近可互动家具时，可以按 F 尝试互动。",
                "把鼠标停在家具上，看看它的描边与小小描述。\n移开鼠标，提示就会收起。\n点击仍保留家具原本的互动或页面入口。",
                "点击两间房中间的小门，再操控桌宠走过去。\n进入对方房间后，可以选择「和 TA 说话」或「先不打扰」。\n对方会怎样回应，要看 ta 当时的心情。",
                "关系变好之后，房间里也许会出现纸条、小物件或礼物。\n发现新东西时，可以点击查看。\n以后想重看这份说明，点右下角「新手引导」即可。"
            };
            string[][] paths = {
                new[]{ Art+"Sprites/Pet/天使/Frames/Move/正面行走/背面行走（上色）.png", Art+"Sprites/Pet/恶魔/Move/正面行走/正面行走（上色）.png" },
                new[]{ Art+"补充/Space_sys/mood_icon.png", Art+"补充/Space_sys/energy_icon.png" },
                new[]{ Art+"Sprites/Pet/天使/Frames/Move/正面行走/背面行走（上色）.png", Art+"Sprites/Pet/恶魔/Move/正面行走/正面行走（上色）.png" },
                new[]{ "", "" },
                new[]{ Art+"UI/Communication/chat_angel.png", Art+"UI/Communication/nochat_devil.png" },
                new[]{ Art+"Sprites/Relic/纸条.png", Art+"Sprites/Relic/千纸鹤.png" }
            };
            var selection = UnityEngine.Object.FindFirstObjectByType<ApartmentFurnitureSelectionPresenter>(FindObjectsInactive.Include);
            for (int i = 0; i < pages.Length; i++)
            {
                var page = Rect(card, "TutorialPage_" + (i + 1), Vector2.zero, Vector2.zero); Stretch(page); pages[i] = page.gameObject;
                Text(page, "Title", titles[i], new Vector2(0, 308), new Vector2(1100, 80), 44);
                Text(page, "Body", bodies[i], new Vector2(0, 142), new Vector2(1120, 210), 30);
                for (int j = 0; j < 2; j++)
                {
                    Sprite? sprite = paths[i][j].Length > 0 ? Load(paths[i][j]) : null;
                    if (sprite == null && selection != null)
                    {
                        int index = i == 3 ? (j == 0 ? 2 : 7) : (j == 0 ? 1 : 9);
                        sprite = selection.Entries[index].Target!.GetComponent<SpriteRenderer>().sprite;
                    }
                    var picture = Rect(page, "TutorialIllustration_" + (i + 1) + "_" + j, new Vector2(j == 0 ? -240 : 240, -120), new Vector2(350, 260));
                    Image(picture, sprite).preserveAspect = true;
                }
                if (i == 2) Text(page, "MovementKeys", "W A S D   /   ↑ ← ↓ →", new Vector2(0, -282), new Vector2(900, 55), 28);
                page.gameObject.SetActive(i == 0);
            }
            var previous = SpriteButton(card, "Btn_TutorialPrevious", Art+"新手引导/left.png", new Vector2(-505, -330), new Vector2(110, 62));
            var next = SpriteButton(card, "Btn_TutorialNext", Art+"新手引导/right.png", new Vector2(505, -330), new Vector2(90, 78));
            var finish = Button(card, "Btn_TutorialFinish", "开始探索", new Vector2(468, -330), new Vector2(190, 62));
            var skip = Button(card, "Btn_TutorialSkip", "跳过引导", new Vector2(466, 318), new Vector2(174, 54));
            var number = Text(card, "TutorialPageNumber", "1 / 6", new Vector2(0, -330), new Vector2(160, 50), 24);
            Bind(open, controller.Open); Bind(previous, controller.Previous); Bind(next, controller.Next); Bind(finish, controller.Finish); Bind(skip, controller.Skip);
            var so = new SerializedObject(controller);
            so.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            var array = so.FindProperty("_pages"); array.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            so.FindProperty("_previous").objectReferenceValue = previous;
            so.FindProperty("_next").objectReferenceValue = next;
            so.FindProperty("_finish").objectReferenceValue = finish;
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_pageNumber").objectReferenceValue = number;
            var build = UnityEngine.Object.FindFirstObjectByType<BuildModeController>(FindObjectsInactive.Include);
            so.FindProperty("_buildMode").objectReferenceValue = build;
            so.ApplyModifiedPropertiesWithoutUndo();
            var bridge = new SerializedObject(viewport);
            bridge.FindProperty("_furnitureHover").objectReferenceValue = selection;
            bridge.FindProperty("_buildMode").objectReferenceValue = build;
            bridge.ApplyModifiedPropertiesWithoutUndo();
            finish.gameObject.SetActive(false); previous.interactable = false; panel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException("缺少图片：" + path);
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Author Apartment Onboarding"); go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static Image Image(RectTransform rect, Sprite? sprite)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        private static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art+"Fonts/NotoSansSC_SDF.asset"); text.fontSize = fontSize; text.fontStyle = FontStyles.Bold; text.color = Ink; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size); var image = Image(rect, Load(PanelPath)); image.pixelsPerUnitMultiplier = 6; image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; Text(rect, "Label", label, Vector2.zero, size - new Vector2(24, 10), 23); return button;
        }
        private static Button SpriteButton(Transform parent, string name, string path, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size); var image = Image(rect, Load(path)); image.preserveAspect = true; image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; return button;
        }
        private static void Bind(Button button, UnityAction action) => UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
#endif
