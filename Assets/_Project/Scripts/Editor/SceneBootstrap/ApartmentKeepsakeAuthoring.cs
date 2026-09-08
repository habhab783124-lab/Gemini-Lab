#nullable enable
#if UNITY_EDITOR
using System;
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Apartment 遗留物首轮的增量作者化入口。
    /// 只创建本功能专属节点，所有 Sprite、布局、碰撞体和 Presenter 引用都保存到 Scene。
    /// </summary>
    public static class ApartmentKeepsakeAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string PaperAPath = "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_散落的纸张_恶魔_02.png";
        private const string PaperBPath = "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_散乱的纸张_恶魔_01.png";
        private const string AngelSignPath = "Assets/_Project/Art/WorldMap/ui1.0/木牌/天使.png";
        private const string DevilSignPath = "Assets/_Project/Art/WorldMap/ui1.0/木牌/恶魔.png";
        private const string CollectionIconPath = "Assets/_Project/Art/WorldMap/ui1.0/icon/图鉴.png";
        private const string BoardPath = "Assets/_Project/Art/Sprites/Collection/collection_system/collection_board.png";

        private static readonly string[] MementoIds =
        {
            "memento.angel.prayer_statue",
            "memento.angel.feather_pen",
            "memento.angel.round_mirror",
            "memento.devil.pink_doll",
            "memento.devil.bat_figure",
            "memento.devil.skull_plush"
        };

        private static readonly string[] MementoSpritePaths =
        {
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_桌面雕塑左_天使_01.png",
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_左下窄摆件_天使_01.png",
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_小圆镜_天使_01.png",
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_中下小摆件_恶魔_02.png",
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_右下小摆件_恶魔_01.png",
            "Assets/_Project/Art/Sprites/Furniture/Decoration/家具_装饰_床上的玩偶_恶魔_01.png"
        };

        private static readonly string[] GiftIds =
        {
            "gift.angel.badge",
            "gift.angel.acrylic_sign",
            "gift.angel.photo",
            "gift.devil.badge",
            "gift.devil.polaroid",
            "gift.devil.postcard"
        };

        private static readonly string[] GiftSpritePaths =
        {
            "Assets/_Project/Art/Sprites/Collection/collection_system/angel_badge.png",
            "Assets/_Project/Art/Sprites/Collection/collection_system/Acrylic sign.png",
            "Assets/_Project/Art/Sprites/Collection/collection_system/photo.png",
            "Assets/_Project/Art/Sprites/Collection/collection_system/evil_badge.png",
            "Assets/_Project/Art/Sprites/Collection/collection_system/Polaroid.png",
            "Assets/_Project/Art/Sprites/Collection/collection_system/postcard.png"
        };

        [MenuItem("Tools/Gemini-Lab/Apartment/Setup Keepsakes")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[ApartmentKeepsakeAuthoring] 当前处于 PlayMode，跳过场景作者化。");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject artGenerated = FindSceneObject("ArtGenerated");
            GameObject sidebar = FindSceneObject("UI_Sidebar");
            if (artGenerated == null || sidebar == null)
            {
                Debug.LogError("[ApartmentKeepsakeAuthoring] 未找到 ArtGenerated 或 UI_Sidebar，未修改场景。");
                return;
            }

            Sprite paperA = LoadSprite(PaperAPath);
            Sprite paperB = LoadSprite(PaperBPath);
            Sprite angelSign = LoadSprite(AngelSignPath);
            Sprite devilSign = LoadSprite(DevilSignPath);
            Sprite collectionIcon = LoadSprite(CollectionIconPath);
            Sprite board = LoadSprite(BoardPath);
            if (paperA == null || paperB == null || angelSign == null || devilSign == null || collectionIcon == null || board == null)
            {
                Debug.LogError("[ApartmentKeepsakeAuthoring] 遗留物首轮依赖的 Sprite 不完整，未修改场景。");
                return;
            }

            GameObject worldRoot = EnsureChild(artGenerated.transform, "ApartmentKeepsakeWorldPresentation");
            var presenter = EnsureComponent<ApartmentKeepsakePresenter>(worldRoot);
            NoteBinding[] notes = BuildNotes(worldRoot.transform, paperA, paperB);
            MementoBinding[] mementos = BuildMementos(worldRoot.transform);

            GameObject overlay = EnsureChild(sidebar.transform, "ApartmentKeepsakeOverlay");
            Canvas canvas = EnsureComponent<Canvas>(overlay);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 103;
            EnsureComponent<GraphicRaycaster>(overlay);

            GameObject detailPanel = EnsurePanel(overlay.transform, "KeepsakeDetailPopup", board, new Vector2(0f, 0f), new Vector2(720f, 520f));
            GameObject detailTitle = EnsureText(detailPanel.transform, "DetailTitle", "遗留物", 30, new Vector2(0f, 150f), new Vector2(600f, 70f));
            GameObject detailBody = EnsureText(detailPanel.transform, "DetailBody", "", 24, new Vector2(0f, 10f), new Vector2(580f, 220f));
            GameObject angelSignObject = EnsureImage(detailPanel.transform, "AngelWoodSign", angelSign, new Vector2(-255f, 165f), new Vector2(150f, 170f));
            GameObject devilSignObject = EnsureImage(detailPanel.transform, "DevilWoodSign", devilSign, new Vector2(255f, 165f), new Vector2(150f, 170f));
            GameObject closeObject = EnsureButton(detailPanel.transform, "CloseButton", "关闭", new Vector2(0f, -185f), new Vector2(180f, 58f));
            detailPanel.SetActive(false);

            GameObject giftButtonRoot = EnsureImage(overlay.transform, "KeepsakeGiftCollectionButton", collectionIcon, new Vector2(300f, 245f), new Vector2(86f, 86f));
            Button giftButton = EnsureComponent<Button>(giftButtonRoot);
            GameObject giftPanel = EnsurePanel(overlay.transform, "KeepsakeGiftCollectionPanel", board, new Vector2(0f, 0f), new Vector2(760f, 600f));
            GameObject giftClose = EnsureButton(giftPanel.transform, "CloseButton", "关闭", new Vector2(0f, -260f), new Vector2(180f, 58f));
            giftPanel.SetActive(false);

            var giftSlots = new GameObject[GiftIds.Length];
            for (int i = 0; i < GiftIds.Length; i++)
            {
                float x = i % 3 * 220f - 220f;
                float y = i / 3 * -180f + 150f;
                Sprite giftSprite = LoadSprite(GiftSpritePaths[i]);
                giftSlots[i] = EnsureImage(giftPanel.transform, $"GiftSlot_{i + 1}", null!, new Vector2(x, y), new Vector2(180f, 140f));
                Image slotBackground = giftSlots[i].GetComponent<Image>();
                if (slotBackground != null)
                {
                    slotBackground.sprite = null;
                }
                EnsureImage(giftSlots[i].transform, "OwnedVisual", giftSprite, Vector2.zero, new Vector2(160f, 112f));
                GameObject lockedVisual = EnsureImage(giftSlots[i].transform, "LockedVisual", giftSprite, Vector2.zero, new Vector2(160f, 112f));
                Image lockedImage = lockedVisual.GetComponent<Image>();
                if (lockedImage != null)
                {
                    lockedImage.color = new Color(0.35f, 0.35f, 0.35f, 0.75f);
                }
                EnsureComponent<Button>(giftSlots[i]);
                EnsureText(giftSlots[i].transform, "Title", "未解锁", 18, new Vector2(0f, -62f), new Vector2(170f, 32f));
            }

            BindPresenter(presenter, notes, mementos, detailPanel, detailTitle, detailBody, angelSignObject, devilSignObject, closeObject,
                giftButtonRoot, giftButton, giftPanel, giftClose, giftSlots);
            BindViewportBridge(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApartmentKeepsakeAuthoring] 遗留物世界节点、详情弹窗、赠礼收藏面板和视口点击桥已作者化。");
        }

        private static NoteBinding[] BuildNotes(Transform parent, Sprite paperA, Sprite paperB)
        {
            var result = new NoteBinding[4];
            Vector3[] positions = { new(-11f, -5.2f, 0f), new(-4f, -5.6f, 0f), new(4f, -5.6f, 0f), new(11f, -5.2f, 0f) };
            for (int i = 0; i < result.Length; i++)
            {
                GameObject go = EnsureChild(parent, $"KeepsakeNoteSpawn_{i + 1}");
                go.transform.localPosition = positions[i];
                go.transform.localScale = Vector3.one * 0.8f;
                SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(go);
                renderer.sprite = i % 2 == 0 ? paperA : paperB;
                renderer.sortingOrder = 120 + i;
                BoxCollider2D collider = EnsureComponent<BoxCollider2D>(go);
                collider.isTrigger = true;
                collider.size = renderer.sprite == null ? Vector2.one : renderer.sprite.bounds.size;
                go.SetActive(false);
                result[i] = new NoteBinding(go, collider);
            }

            return result;
        }

        private static MementoBinding[] BuildMementos(Transform parent)
        {
            var result = new MementoBinding[MementoIds.Length];
            Vector3[] positions = { new(-8f, -3.8f, 0f), new(-4f, -3.8f, 0f), new(0f, -3.8f, 0f), new(4f, -3.8f, 0f), new(8f, -3.8f, 0f), new(12f, -3.8f, 0f) };
            for (int i = 0; i < result.Length; i++)
            {
                GameObject go = EnsureChild(parent, $"KeepsakeMemento_{i + 1}");
                go.transform.localPosition = positions[i];
                go.transform.localScale = Vector3.one * 0.75f;
                SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(go);
                renderer.sprite = LoadSprite(MementoSpritePaths[i]);
                renderer.sortingOrder = 130 + i;
                BoxCollider2D collider = EnsureComponent<BoxCollider2D>(go);
                collider.isTrigger = true;
                collider.size = renderer.sprite == null ? Vector2.one : renderer.sprite.bounds.size;
                go.SetActive(false);
                result[i] = new MementoBinding(MementoIds[i], go, collider);
            }

            return result;
        }

        private static void BindPresenter(
            ApartmentKeepsakePresenter presenter,
            NoteBinding[] notes,
            MementoBinding[] mementos,
            GameObject detailPanel,
            GameObject detailTitle,
            GameObject detailBody,
            GameObject angelSign,
            GameObject devilSign,
            GameObject detailClose,
            GameObject giftButtonRoot,
            Button giftButton,
            GameObject giftPanel,
            GameObject giftClose,
            GameObject[] giftSlots)
        {
            var so = new SerializedObject(presenter);
            SetArray(so.FindProperty("_noteSpawns"), notes.Length, (element, i) =>
            {
                SetObject(element.FindPropertyRelative("Root"), notes[i].Root);
                SetObject(element.FindPropertyRelative("ClickCollider"), notes[i].Collider);
            });
            SetArray(so.FindProperty("_mementoVisuals"), mementos.Length, (element, i) =>
            {
                SetString(element.FindPropertyRelative("ItemId"), mementos[i].ItemId);
                SetObject(element.FindPropertyRelative("Root"), mementos[i].Root);
                SetObject(element.FindPropertyRelative("ClickCollider"), mementos[i].Collider);
            });
            SetObject(so.FindProperty("_detailPopupRoot"), detailPanel);
            SetObject(so.FindProperty("_detailTitleText"), detailTitle.GetComponent<TMP_Text>());
            SetObject(so.FindProperty("_detailBodyText"), detailBody.GetComponent<TMP_Text>());
            SetObject(so.FindProperty("_angelWoodSign"), angelSign);
            SetObject(so.FindProperty("_devilWoodSign"), devilSign);
            SetObject(so.FindProperty("_detailCloseButton"), detailClose.GetComponent<Button>());
            SetObject(so.FindProperty("_giftCollectionButtonRoot"), giftButtonRoot);
            SetObject(so.FindProperty("_giftCollectionButton"), giftButton);
            SetObject(so.FindProperty("_giftCollectionPanelRoot"), giftPanel);
            SetObject(so.FindProperty("_giftCollectionCloseButton"), giftClose.GetComponent<Button>());
            SetArray(so.FindProperty("_giftSlots"), giftSlots.Length, (element, i) =>
            {
                SetString(element.FindPropertyRelative("ItemId"), GiftIds[i]);
                SetObject(element.FindPropertyRelative("Root"), giftSlots[i]);
                SetObject(element.FindPropertyRelative("OwnedVisual"), giftSlots[i].transform.Find("OwnedVisual")?.gameObject);
                SetObject(element.FindPropertyRelative("LockedVisual"), giftSlots[i].transform.Find("LockedVisual")?.gameObject);
                SetObject(element.FindPropertyRelative("DetailButton"), giftSlots[i].GetComponent<Button>());
                SetObject(element.FindPropertyRelative("TitleText"), giftSlots[i].transform.Find("Title")?.GetComponent<TMP_Text>());
            });
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void BindViewportBridge(ApartmentKeepsakePresenter presenter)
        {
            ApartmentViewportInputBridge? bridge = Object.FindFirstObjectByType<ApartmentViewportInputBridge>(FindObjectsInactive.Include);
            GameObject? imageObject = FindSceneObject("ApartmentViewportImage");
            GameObject? cameraObject = FindSceneObject("ApartmentViewportCamera");
            if (bridge == null || imageObject == null || cameraObject == null)
            {
                Debug.LogWarning("[ApartmentKeepsakeAuthoring] 未找到视口桥、RawImage 或 Camera，保留 Presenter 但未绑定点击桥。");
                return;
            }

            var so = new SerializedObject(bridge);
            SetObject(so.FindProperty("_viewportImage"), imageObject.GetComponent<RawImage>());
            SetObject(so.FindProperty("_viewportCamera"), cameraObject.GetComponent<Camera>());
            SerializedProperty handlers = so.FindProperty("_worldPointInteractables");
            if (handlers != null)
            {
                handlers.arraySize = 1;
                handlers.GetArrayElementAtIndex(0).objectReferenceValue = presenter;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bridge);
        }

        private static GameObject EnsurePanel(Transform parent, string name, Sprite background, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name);
            RectTransform rect = EnsureComponent<RectTransform>(go);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = EnsureComponent<Image>(go);
            image.sprite = background;
            image.preserveAspect = true;
            return go;
        }

        private static GameObject EnsureImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name);
            RectTransform rect = EnsureComponent<RectTransform>(go);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = EnsureComponent<Image>(go);
            image.sprite = sprite;
            image.preserveAspect = true;
            return go;
        }

        private static GameObject EnsureButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureImage(parent, name, null!, position, size);
            EnsureComponent<Button>(go);
            EnsureText(go.transform, "Label", label, 20, Vector2.zero, size);
            return go;
        }

        private static GameObject EnsureText(Transform parent, string name, string text, float fontSize, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name, typeof(RectTransform));
            RectTransform rect = EnsureComponent<RectTransform>(go);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(go);
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return go;
        }

        private static GameObject EnsureChild(Transform parent, string name, params Type[] componentTypes)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = componentTypes.Length == 0 ? new GameObject(name) : new GameObject(name, componentTypes);
            Undo.RegisterCreatedObjectUndo(go, "Create Apartment keepsake node");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T? component = go.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            T? added = Undo.AddComponent<T>(go);
            return added != null ? added : go.AddComponent<T>();
        }

        private static GameObject? FindSceneObject(string name)
        {
            GameObject? exact = GameObject.Find(name);
            if (exact != null)
            {
                return exact;
            }

            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate.name == name && candidate.gameObject.scene.IsValid())
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static Sprite? LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void SetObject(SerializedProperty? property, Object? value)
        {
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetString(SerializedProperty? property, string value)
        {
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetArray(SerializedProperty? property, int length, Action<SerializedProperty, int> fill)
        {
            if (property == null)
            {
                return;
            }

            property.arraySize = length;
            for (int i = 0; i < length; i++)
            {
                fill(property.GetArrayElementAtIndex(i), i);
            }
        }

        private readonly struct NoteBinding
        {
            public NoteBinding(GameObject root, Collider2D collider) { Root = root; Collider = collider; }
            public GameObject Root { get; }
            public Collider2D Collider { get; }
        }

        private readonly struct MementoBinding
        {
            public MementoBinding(string itemId, GameObject root, Collider2D collider) { ItemId = itemId; Root = root; Collider = collider; }
            public string ItemId { get; }
            public GameObject Root { get; }
            public Collider2D Collider { get; }
        }
    }
}
#endif
