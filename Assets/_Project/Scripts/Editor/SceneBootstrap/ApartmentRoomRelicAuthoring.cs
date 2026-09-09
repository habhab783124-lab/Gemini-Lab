#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.RoomRelic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentRoomRelicAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string CatalogFolder = "Assets/_Project/ScriptableObjects/RoomRelicConfig";
        private const string CatalogPath = CatalogFolder + "/RoomRelicCatalog.asset";
        private const string RelicRootName = "RoomRelic";

        private const int NoteSlotCount = 3;
        private const int RelicSlotCount = 5;
        private const int GiftSlotCount = 3;

        [MenuItem("Tools/Gemini-Lab/Apartment/Upgrade Room Relic Bindings")]
        public static void UpgradeExistingBindings()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlaying)
            {
                Debug.LogWarning("[RoomRelicAuthoring] 请在编辑模式打开 Apartment_Main 后升级绑定。");
                return;
            }

            int updated = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (RoomRelicRoomView room in root.GetComponentsInChildren<RoomRelicRoomView>(true))
            {
                SerializedObject roomObject = new(room);
                SerializedProperty slots = roomObject.FindProperty("_giftSlots");
                for (int i = 0; i < Math.Min(2, slots.arraySize); i++)
                {
                    var slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as RoomRelicView;
                    if (slot == null || !string.IsNullOrEmpty(slot.DisplaySlotId)) continue;
                    Undo.RecordObject(slot, "Bind room gift display slot");
                    SerializedObject slotObject = new(slot);
                    slotObject.FindProperty("_displaySlotId").stringValue = i == 0 ? "desk" : "shelf";
                    slotObject.ApplyModifiedProperties();
                    updated++;
                }
            }

            RoomRelicCatalogSO? catalog = AssetDatabase.LoadAssetAtPath<RoomRelicCatalogSO>(CatalogPath);
            if (catalog != null)
            {
                SerializedObject catalogObject = new(catalog);
                SerializedProperty notes = catalogObject.FindProperty("notes");
                RoomNoteData[] defaults = CreatePlaceholderNotes();
                for (int i = 0; i < notes.arraySize; i++)
                {
                    SerializedProperty note = notes.GetArrayElementAtIndex(i);
                    SerializedProperty content = note.FindPropertyRelative("content");
                    if (!content.stringValue.StartsWith("【占位】", StringComparison.Ordinal)) continue;
                    foreach (RoomNoteData replacement in defaults)
                    {
                        if (note.FindPropertyRelative("id").stringValue != replacement.id) continue;
                        content.stringValue = replacement.content;
                        break;
                    }
                }
                catalogObject.ApplyModifiedProperties();
            }
            if (updated > 0) EditorSceneManager.MarkSceneDirty(scene);
            // 不自动保存，保留 Undo 并允许作者检查；调用方可在核对后保存。
            Debug.Log($"[RoomRelicAuthoring] 升级 {updated} 个赠礼槽位，已保留位置、素材和自定义文案。");
        }

        [MenuItem("Tools/Gemini-Lab/Apartment/Author Room Relic")]
        public static void Author()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    {
                        Debug.LogWarning("[RoomRelicAuthoring] 存在未保存场景，请保存后再作者化。");
                        return;
                    }
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            GameObject? artRoot = GameObject.Find("ArtGenerated");
            if (artRoot == null)
            {
                Debug.LogError("[RoomRelicAuthoring] 未找到 ArtGenerated，无法继续。");
                return;
            }

            RoomRelicCatalogSO catalog = CreateOrUpdatePlaceholderCatalog();
            GameObject relicRoot = EnsureChild(artRoot.transform, RelicRootName);
            EnsureRuntimeBootstrap(relicRoot, catalog);

            EnsureRoom(relicRoot.transform, RoomId.AngelRoom, catalog);
            EnsureRoom(relicRoot.transform, RoomId.DevilRoom, catalog);
            EnsureEntryTriggers();
            EnsurePopups(catalog);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoomRelicAuthoring] RoomRelic 场景占位作者化完成。");
        }

        private static RoomRelicCatalogSO CreateOrUpdatePlaceholderCatalog()
        {
            EnsureAssetFolder(CatalogFolder);
            RoomRelicCatalogSO? catalog = AssetDatabase.LoadAssetAtPath<RoomRelicCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoomRelicCatalogSO>();
                catalog.notes = CreatePlaceholderNotes();
                catalog.relics = CreatePlaceholderRelics();
                catalog.gifts = CreatePlaceholderGifts();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            return catalog;
        }

        private static void EnsureRuntimeBootstrap(GameObject relicRoot, RoomRelicCatalogSO catalog)
        {
            RoomRelicRuntimeBootstrap bootstrap = relicRoot.GetComponent<RoomRelicRuntimeBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = relicRoot.AddComponent<RoomRelicRuntimeBootstrap>();
            }

            SerializedObject so = new(bootstrap);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureRoom(Transform parent, RoomId roomId, RoomRelicCatalogSO catalog)
        {
            string roomName = roomId == RoomId.AngelRoom ? "AngelRoom" : "DevilRoom";
            // 已作者化的房间保留位置、变体、引用和人工布局，不重新随机摆放。
            if (parent.Find(roomName) != null) return;
            GameObject roomRoot = EnsureChild(parent, roomName);
            RoomRelicRoomView roomView = roomRoot.GetComponent<RoomRelicRoomView>();
            if (roomView == null)
            {
                roomView = roomRoot.AddComponent<RoomRelicRoomView>();
            }

            SetIntField(roomView, "_roomId", (int)roomId);

            string sender = roomId == RoomId.AngelRoom ? "Demon" : "Angel";
            string receiver = roomId == RoomId.AngelRoom ? "Angel" : "Demon";
            Sprite sprite = GetPlaceholderSprite();

            float dir = roomId == RoomId.AngelRoom ? 1f : -1f;
            float centerX = 8.9f * dir;
            float centerY = -1.25f;
            System.Random rng = new System.Random(roomId == RoomId.AngelRoom ? 137 : 731);

            Vector2[] notePositions = new Vector2[NoteSlotCount];
            for (int i = 0; i < notePositions.Length; i++)
            {
                notePositions[i] = RandomSlotPosition(rng, centerX, centerY);
            }

            Vector2[] relicPositions = new Vector2[RelicSlotCount];
            for (int i = 0; i < relicPositions.Length; i++)
            {
                relicPositions[i] = RandomSlotPosition(rng, centerX, centerY);
            }

            Vector2[] giftPositions = new Vector2[GiftSlotCount];
            for (int i = 0; i < giftPositions.Length; i++)
            {
                giftPositions[i] = RandomSlotPosition(rng, centerX, centerY);
            }

            Transform noteContainer = EnsureChild(roomRoot.transform, "NoteSpawns").transform;
            string[] noteIds = new string[catalog.notes.Length];
            int noteCount = 0;
            for (int i = 0; i < catalog.notes.Length; i++)
            {
                if (catalog.notes[i].senderCharacter == sender)
                {
                    noteIds[noteCount++] = catalog.notes[i].id;
                }
            }

            Array.Resize(ref noteIds, noteCount);
            Dictionary<string, Sprite> noteSprites = new();
            for (int i = 0; i < catalog.notes.Length; i++)
            {
                RoomNoteData note = catalog.notes[i];
                if (note.senderCharacter != sender)
                {
                    continue;
                }

                Sprite? loaded = LoadNoteSpriteByVisualType(note.visualType);
                if (loaded != null)
                {
                    noteSprites[note.id] = loaded;
                }
            }

            RoomRelicView[] noteViews = new RoomRelicView[NoteSlotCount];
            for (int i = 0; i < NoteSlotCount; i++)
            {
                noteViews[i] = CreateSlot(
                    $"NoteSpawn_{i:D2}",
                    noteContainer,
                    roomId,
                    RoomRelicKind.Note,
                    noteIds,
                    sprite,
                    new Color(1f, 0.92f, 0.55f, 1f),
                    notePositions[i],
                    noteSprites);
            }

            Transform relicContainer = EnsureChild(roomRoot.transform, "RelicSpawns").transform;
            string[] relicIds = new string[catalog.relics.Length];
            int relicCount = 0;
            for (int i = 0; i < catalog.relics.Length; i++)
            {
                if (catalog.relics[i].targetRoom == roomId &&
                    catalog.relics[i].ownerCharacter == sender)
                {
                    relicIds[relicCount++] = catalog.relics[i].id;
                }
            }

            Array.Resize(ref relicIds, relicCount);
            Dictionary<string, Sprite> relicSprites = new();
            for (int i = 0; i < catalog.relics.Length; i++)
            {
                RoomRelicData relic = catalog.relics[i];
                if (relic.targetRoom != roomId || relic.ownerCharacter != sender ||
                    string.IsNullOrWhiteSpace(relic.roomVisualKey))
                {
                    continue;
                }

                Sprite? loadedSprite = LoadRelicSpriteByGuid(relic.roomVisualKey);
                if (loadedSprite != null)
                {
                    relicSprites[relic.id] = loadedSprite;
                }
            }

            RoomRelicView[] relicViews = new RoomRelicView[RelicSlotCount];
            for (int i = 0; i < RelicSlotCount; i++)
            {
                relicViews[i] = CreateSlot(
                    $"RelicSpawn_{i:D2}",
                    relicContainer,
                    roomId,
                    RoomRelicKind.TemporaryRelic,
                    relicIds,
                    sprite,
                    new Color(0.55f, 0.8f, 1f, 1f),
                    relicPositions[i],
                    relicSprites);
            }

            Transform giftContainer = EnsureChild(roomRoot.transform, "GiftSlots").transform;
            string[] giftIds = new string[catalog.gifts.Length];
            int giftCount = 0;
            for (int i = 0; i < catalog.gifts.Length; i++)
            {
                if (catalog.gifts[i].receiverCharacter == receiver)
                {
                    giftIds[giftCount++] = catalog.gifts[i].id;
                }
            }

            Array.Resize(ref giftIds, giftCount);
            Dictionary<string, Sprite> giftSprites = new();
            for (int i = 0; i < catalog.gifts.Length; i++)
            {
                RoomGiftData gift = catalog.gifts[i];
                if (gift.receiverCharacter != receiver ||
                    string.IsNullOrWhiteSpace(gift.roomVisualKey))
                {
                    continue;
                }

                Sprite? loaded = LoadRelicSpriteByGuid(gift.roomVisualKey);
                if (loaded != null)
                {
                    giftSprites[gift.id] = loaded;
                }
            }

            RoomRelicView[] giftViews = new RoomRelicView[GiftSlotCount];
            for (int i = 0; i < GiftSlotCount; i++)
            {
                giftViews[i] = CreateSlot(
                    $"GiftSlot_{i:D2}",
                    giftContainer,
                    roomId,
                    RoomRelicKind.PermanentGift,
                    giftIds,
                    sprite,
                    new Color(1f, 0.72f, 0.86f, 1f),
                    giftPositions[i],
                    giftSprites);
                SerializedObject giftSlotObject = new(giftViews[i]);
                giftSlotObject.FindProperty("_displaySlotId").stringValue = i == 0 ? "desk" : i == 1 ? "shelf" : string.Empty;
                giftSlotObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetObjectArray(roomView, "_noteSlots", noteViews);
            SetObjectArray(roomView, "_relicSlots", relicViews);
            SetObjectArray(roomView, "_giftSlots", giftViews);
        }

        private static Vector2 RandomSlotPosition(System.Random rng, float centerX, float centerY)
        {
            float x = centerX + ((float)rng.NextDouble() * 2f - 1f) * 3.5f;
            float y = centerY + ((float)rng.NextDouble() * 2f - 1f) * 3f;
            return new Vector2(x, y);
        }

        private static RoomRelicView CreateSlot(
            string name,
            Transform parent,
            RoomId roomId,
            RoomRelicKind kind,
            string[] variantIds,
            Sprite sprite,
            Color color,
            Vector2 position,
            Dictionary<string, Sprite>? spriteOverrides = null)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.layer = parent.gameObject.layer;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.35f, 1.35f);

            RoomRelicView view = root.AddComponent<RoomRelicView>();
            RoomRelicInteraction interaction = root.AddComponent<RoomRelicInteraction>();
            SetIntField(interaction, "_roomId", (int)roomId);
            SetIntField(interaction, "_kind", (int)kind);

            GameObject[] targets = new GameObject[variantIds.Length];
            for (int i = 0; i < variantIds.Length; i++)
            {
                GameObject variant = new(variantIds[i]);
                variant.transform.SetParent(root.transform, false);
                variant.layer = parent.gameObject.layer;
                variant.SetActive(false);

                SpriteRenderer renderer = variant.AddComponent<SpriteRenderer>();
                Sprite variantSprite = sprite;
                if (spriteOverrides != null &&
                    spriteOverrides.TryGetValue(variantIds[i], out Sprite? overriddenSprite) &&
                    overriddenSprite != null)
                {
                    variantSprite = overriddenSprite;
                }

                renderer.sprite = variantSprite;
                renderer.color = color;
                renderer.sortingOrder = 200;
                targets[i] = variant;
            }

            SetVariantBindings(view, targets, variantIds);
            return view;
        }

        private static void EnsureEntryTriggers()
        {
            EnsureEntryTrigger("PetMovementBounds", RoomId.AngelRoom, PetId.Angel);
            EnsureEntryTrigger("PetMovementBounds_Devil", RoomId.DevilRoom, PetId.Devil);
        }

        private static void EnsureEntryTrigger(string boundsName, RoomId roomId, PetId petId)
        {
            GameObject? bounds = GameObject.Find(boundsName);
            if (bounds == null)
            {
                Debug.LogWarning($"[RoomRelicAuthoring] 未找到 {boundsName}，跳过房间触发器。");
                return;
            }

            RoomRelicEntryTrigger trigger = bounds.GetComponent<RoomRelicEntryTrigger>();
            if (trigger == null)
            {
                trigger = bounds.AddComponent<RoomRelicEntryTrigger>();
            }

            SetIntField(trigger, "_roomId", (int)roomId);
            SetIntField(trigger, "_expectedPetId", (int)petId);
        }

        private static void EnsurePopups(RoomRelicCatalogSO catalog)
        {
            GameObject? uiRoot = GameObject.Find("UI_Sidebar");
            if (uiRoot == null)
            {
                Debug.LogWarning("[RoomRelicAuthoring] 未找到 UI_Sidebar，跳过弹窗作者化。");
                return;
            }

            CreatePopup<RoomNotePopup>(uiRoot.transform, "RoomNotePopup", catalog);
            CreatePopup<RoomRelicDetailPopup>(uiRoot.transform, "RoomRelicDetailPopup", catalog);
            CreatePopup<RoomGiftObtainedPopup>(uiRoot.transform, "RoomGiftObtainedPopup", catalog);
        }

        private static void CreatePopup<T>(Transform parent, string name, RoomRelicCatalogSO catalog) where T : RoomRelicPanelBase
        {
            if (parent.Find(name) != null) return;
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.layer = parent.gameObject.layer;

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject content = new("Content");
            content.transform.SetParent(root.transform, false);
            content.layer = parent.gameObject.layer;

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(640f, 420f);

            Image panelBg = content.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.13f, 0.18f, 0.98f);

            Button closeButton = CreateCloseButton(content.transform);
            TMP_Text title = CreateText(content.transform, "Title", 42, TextAlignmentOptions.Center);
            SetAnchoredRect(title.rectTransform, new Vector2(0f, 160f), new Vector2(560f, 60f));

            TMP_Text body = CreateText(content.transform, "Body", 36, TextAlignmentOptions.TopLeft);
            SetAnchoredRect(body.rectTransform, new Vector2(0f, -30f), new Vector2(560f, 260f));

            T component = root.AddComponent<T>();
            SerializedObject so = new(component);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;

            if (component is RoomNotePopup)
            {
                title.text = "一张纸条";
                so.FindProperty("_contentText").objectReferenceValue = body;
            }
            else if (component is RoomRelicDetailPopup)
            {
                so.FindProperty("_nameText").objectReferenceValue = title;
                so.FindProperty("_descriptionText").objectReferenceValue = body;
                RoomRelicView iconView = CreateIconVariantView(content.transform, "IconView",
                    BuildIconItems(catalog.relics, relic => relic.id, relic => relic.roomVisualKey));
                SetAnchoredRect(title.rectTransform, new Vector2(0f, 145f), new Vector2(470f, 60f));
                SetAnchoredRect(body.rectTransform, new Vector2(0f, -135f), new Vector2(560f, 100f));
                so.FindProperty("_iconView").objectReferenceValue = iconView;
            }
            else if (component is RoomGiftObtainedPopup)
            {
                so.FindProperty("_giftNameText").objectReferenceValue = title;
                so.FindProperty("_hintText").objectReferenceValue = body;
                RoomRelicView iconView = CreateIconVariantView(content.transform, "IconView",
                    BuildIconItems(catalog.gifts, gift => gift.id, gift => gift.roomVisualKey));
                SetAnchoredRect(title.rectTransform, new Vector2(0f, 145f), new Vector2(470f, 60f));
                SetAnchoredRect(body.rectTransform, new Vector2(0f, -135f), new Vector2(560f, 70f));
                body.alignment = TextAlignmentOptions.Center;
                so.FindProperty("_iconView").objectReferenceValue = iconView;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            content.SetActive(false);
        }

        private static RoomRelicView CreateIconVariantView(
            Transform parent,
            string name,
            (string id, string visualKey)[] items)
        {
            GameObject iconRoot = new(name);
            iconRoot.transform.SetParent(parent, false);
            iconRoot.layer = parent.gameObject.layer;

            RectTransform rect = iconRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 15f);
            rect.sizeDelta = new Vector2(160f, 160f);

            RoomRelicView view = iconRoot.AddComponent<RoomRelicView>();

            GameObject[] targets = new GameObject[items.Length];
            string[] ids = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                GameObject variant = new(items[i].id);
                variant.transform.SetParent(iconRoot.transform, false);
                variant.layer = parent.gameObject.layer;
                variant.SetActive(false);

                RectTransform variantRect = variant.AddComponent<RectTransform>();
                variantRect.anchorMin = Vector2.zero;
                variantRect.anchorMax = Vector2.one;
                variantRect.offsetMin = Vector2.zero;
                variantRect.offsetMax = Vector2.zero;

                Image image = variant.AddComponent<Image>();
                image.color = Color.white;
                image.preserveAspect = true;

                Sprite? loaded = LoadRelicSpriteByGuid(items[i].visualKey);
                if (loaded != null)
                {
                    image.sprite = loaded;
                }

                targets[i] = variant;
                ids[i] = items[i].id;
            }

            SetVariantBindings(view, targets, ids);
            return view;
        }

        private static (string id, string visualKey)[] BuildIconItems<T>(
            T[] items,
            Func<T, string> idSelector,
            Func<T, string> visualKeySelector)
        {
            (string id, string visualKey)[] result = new (string, string)[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                result[i] = (idSelector(items[i]), visualKeySelector(items[i]));
            }
            return result;
        }

        private static Sprite? LoadNoteSpriteByVisualType(RoomNoteVisualType visualType)
        {
            string guid = visualType switch
            {
                RoomNoteVisualType.Note => "0d2b9396548d08544bcf409bbd6455a9",
                RoomNoteVisualType.PaperBall => "108dce6d28452e14790c99f555fe4c70",
                _ => string.Empty
            };
            return string.IsNullOrWhiteSpace(guid) ? null : LoadRelicSpriteByGuid(guid);
        }

        private static Button CreateCloseButton(Transform parent)
        {
            GameObject buttonGo = new("CloseButton");
            buttonGo.transform.SetParent(parent, false);
            buttonGo.layer = parent.gameObject.layer;

            RectTransform rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(48f, 48f);

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.16f);
            Button button = buttonGo.AddComponent<Button>();

            TMP_Text label = CreateText(buttonGo.transform, "Label", 44, TextAlignmentOptions.Center);
            label.text = "X";
            StretchRect(label.rectTransform);
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textGo = new(name);
            textGo.transform.SetParent(parent, false);
            textGo.layer = parent.gameObject.layer;

            RectTransform rect = textGo.AddComponent<RectTransform>();
            TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetVariantBindings(RoomRelicView view, GameObject[] targets, string[] ids)
        {
            SerializedObject so = new(view);
            SerializedProperty variants = so.FindProperty("_variants");
            variants.arraySize = ids.Length;

            for (int i = 0; i < ids.Length; i++)
            {
                SerializedProperty element = variants.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_id").stringValue = ids[i];
                element.FindPropertyRelative("_target").objectReferenceValue = targets[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(RoomRelicRoomView view, string fieldName, RoomRelicView[] views)
        {
            SerializedObject so = new(view);
            SerializedProperty array = so.FindProperty(fieldName);
            array.arraySize = views.Length;

            for (int i = 0; i < views.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIntField(UnityEngine.Object target, string fieldName, int value)
        {
            SerializedObject so = new(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null)
            {
                property.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static Sprite GetPlaceholderSprite()
        {
            Sprite? sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        private static Sprite? LoadRelicSpriteByGuid(string assetGuid)
        {
            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null)
            {
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static RoomNoteData[] CreatePlaceholderNotes()
        {
            return new[]
            {
                new RoomNoteData { id = "note_demon_01", senderCharacter = "Demon", receiverCharacter = "Angel", content = "窗边那架纸飞机是我的。你要是想试飞，记得叫上我。", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_demon_02", senderCharacter = "Demon", receiverCharacter = "Angel", content = "刚才那段吉他不是弹错了，是新编的。你笑什么。", visualType = RoomNoteVisualType.PaperBall, weight = 1f },
                new RoomNoteData { id = "note_demon_03", senderCharacter = "Demon", receiverCharacter = "Angel", content = "糖果分你一颗。南瓜形状的那颗……也可以给你。", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_demon_04", senderCharacter = "Demon", receiverCharacter = "Angel", content = "今天的晚霞像打翻的颜料盘。下次一起看吧。", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_angel_01", senderCharacter = "Angel", receiverCharacter = "Demon", content = "借你的书放回去了，夹着羽毛的那页，我想再读一遍。", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_angel_02", senderCharacter = "Angel", receiverCharacter = "Demon", content = "这只纸鹤折歪了。你说像我打瞌睡的时候，所以留下了。", visualType = RoomNoteVisualType.PaperBall, weight = 1f },
                new RoomNoteData { id = "note_angel_03", senderCharacter = "Angel", receiverCharacter = "Demon", content = "听见你练琴了。最后那一小段很好听，可以再弹一次吗？", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_angel_04", senderCharacter = "Angel", receiverCharacter = "Demon", content = "窗台留了一个位置。等星星出来的时候，你也来坐一会儿吧。", visualType = RoomNoteVisualType.Note, weight = 1f }
            };
        }

        private static RoomRelicData[] CreatePlaceholderRelics()
        {
            return new[]
            {
                new RoomRelicData { id = "relic_demon_01", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "红黑纸飞机", observationText = "折得歪歪扭扭的，飞得倒还挺远。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/纸飞机.png", roomVisualKey = "4e070b2a29c2f8b4d832e4d5c174f8b0", weight = 1f },
                new RoomRelicData { id = "relic_demon_02", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "调色盘", observationText = "颜色混得乱七八糟，像他整理到一半的心事。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/颜料盘.png", roomVisualKey = "8930f36f3efe98e468f3522ae96fa1b3", weight = 1f },
                new RoomRelicData { id = "relic_demon_03", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "拨片", observationText = "这个笨蛋不会以为竖琴和吉他一样用拨片吧。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/拨片.png", roomVisualKey = "c7a9a7cd560939b4faf921b6fca7c7f3", weight = 1f },
                new RoomRelicData { id = "relic_demon_04", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "南瓜糖果", observationText = "把最喜欢的万圣限定款送我了？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/南瓜糖果.png", roomVisualKey = "232777944ca791d448cf4840262643c4", weight = 1f },
                new RoomRelicData { id = "relic_demon_05", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "速写", observationText = "把我画的傻乎乎的……扔掉算了——算了。", weight = 1f },
                new RoomRelicData { id = "relic_angel_01", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "千纸鹤", observationText = "翅膀和祂的一样轻一样薄，嗯，你是天使的信使吗？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/千纸鹤.png", roomVisualKey = "b3bc4a7a7b73869419f8897dccbafd8a", weight = 1f },
                new RoomRelicData { id = "relic_angel_02", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "雕花小镜子", observationText = "嗯、嗯、嗯，诶这个镜子没办法和我说话吗？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/小镜子.png", roomVisualKey = "c54d937a2c5f2b84bb6870e8a0bd3c05", weight = 1f },
                new RoomRelicData { id = "relic_angel_03", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "羽毛书签", observationText = "好看……我也想要羽毛翅膀……还能掉下来做书签……", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/羽毛书签.png", roomVisualKey = "6643c15cd7a2ec74fbbe1a119c5d1d01", weight = 1f },
                new RoomRelicData { id = "relic_angel_04", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "竖琴琴谱残页", observationText = "要是祂听到我用吉他弹出这一段会很惊讶吧。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/竖琴残页.png", roomVisualKey = "cedf53eb805316849bcc895b54650888", weight = 1f },
                new RoomRelicData { id = "relic_angel_05", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "小星星吊坠", observationText = "不重，亮晶晶的，系在尾巴尖上刚刚好。", weight = 1f }
            };
        }

        private static RoomGiftData[] CreatePlaceholderGifts()
        {
            return new[]
            {
                new RoomGiftData { id = "gift_demon_01", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "南瓜糖果", observationText = "恶魔送给天使的南瓜糖果。", roomVisualKey = "232777944ca791d448cf4840262643c4", displaySlotId = "desk", weight = 1f },
                new RoomGiftData { id = "gift_demon_02", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "速写", observationText = "恶魔送给天使的速写。", roomVisualKey = "", displaySlotId = "shelf", weight = 1f },
                new RoomGiftData { id = "gift_angel_01", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "羽毛书签", observationText = "天使送给恶魔的羽毛书签。", roomVisualKey = "6643c15cd7a2ec74fbbe1a119c5d1d01", displaySlotId = "desk", weight = 1f },
                new RoomGiftData { id = "gift_angel_02", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "小星星吊坠", observationText = "天使送给恶魔的小星星吊坠。", roomVisualKey = "", displaySlotId = "shelf", weight = 1f }
            };
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            string leaf = folder.Substring(folder.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
