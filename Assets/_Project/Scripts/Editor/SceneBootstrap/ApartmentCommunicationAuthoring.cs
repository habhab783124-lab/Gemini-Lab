#if UNITY_EDITOR
using System.Linq;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.RoomRelic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentCommunicationAuthoring
    {
        public const string ArtFolder = "Assets/_Project/Art/UI/Communication";
        public const string CatalogPath = "Assets/_Project/ScriptableObjects/RoomRelicConfig/RoomRelicCatalog.asset";

        [MenuItem("Tools/Gemini-Lab/Apartment/Update Communication")]
        public static void Author()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Apartment/Apartment_Main.unity")
                throw new System.InvalidOperationException("请在非运行状态打开 Apartment_Main。");
            var door = Object.FindObjectsByType<ApartmentDoorInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene);
            var bridge = Object.FindObjectsByType<ApartmentViewportInputBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.gameObject.scene == scene);
            var viewport = (RawImage)new SerializedObject(bridge).FindProperty("_viewportImage").objectReferenceValue;
            var settings = new SerializedObject(door);
            foreach (string side in new[] { "angel", "devil" })
            {
                var pet = (PetController)settings.FindProperty("_" + side).objectReferenceValue;
                Ref(settings, "_" + side + "Room", new SerializedObject(pet.GetComponent<ApartmentPetMovement>()).FindProperty("_room").objectReferenceValue);
                // 已有场景允许在 Inspector 调整布局，再次执行不覆盖这些调整。
                if (settings.FindProperty("_" + side + "Choices").objectReferenceValue != null) continue;
                var root = new GameObject(side == "angel" ? "AngelConversationChoices" : "DevilConversationChoices", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "Author conversation choices");
                var rect = (RectTransform)root.transform;
                rect.SetParent(viewport.transform, false);
                rect.anchorMin = new Vector2(side == "angel" ? .5f : 0f, 0);
                rect.anchorMax = new Vector2(side == "angel" ? 1f : .5f, 0);
                rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(0, 50); rect.sizeDelta = new Vector2(0, 80);
                Ref(settings, "_" + side + "Choices", root);
                Ref(settings, "_" + side + "Chat", Button(rect, "Chat", "chat_" + side, .22f));
                Ref(settings, "_" + side + "Decline", Button(rect, "Decline", "nochat_" + side, .78f));
                root.SetActive(false);
            }
            settings.ApplyModifiedProperties();
            var catalog = AssetDatabase.LoadAssetAtPath<RoomRelicCatalogSO>(CatalogPath);
            Undo.RecordObject(catalog, "Update planned notes");
            catalog.notes = ApartmentCommunicationContent.CreateNotes();
            EditorUtility.SetDirty(catalog);

            // 每个槽都能展示所有新 ID，保留原有纸条/纸团的尺寸、位置与资源引用。
            foreach (var room in Object.FindObjectsByType<RoomRelicRoomView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.gameObject.scene == scene))
            {
                var roomSettings = new SerializedObject(room);
                string sender = roomSettings.FindProperty("_roomId").intValue == (int)RoomId.AngelRoom ? "Demon" : "Angel";
                var slots = roomSettings.FindProperty("_noteSlots");
                for (int i = 0; i < slots.arraySize; i++)
                    ExpandNoteSlot((RoomRelicView)slots.GetArrayElementAtIndex(i).objectReferenceValue, catalog.notes.Where(n => n.senderCharacter == sender).ToArray());
            }
            Undo.FlushUndoRecordObjects();
            AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Communication] 已绑定双方房间与四个选项按钮，更新 34 条纸条及所有槽位。请保存场景。");
        }

        private static Button Button(Transform parent, string name, string asset, float x)
        {
            string path = ArtFolder + "/" + asset + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.IO.FileNotFoundException(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(x, .5f);
            rect.sizeDelta = new Vector2(300, 80);
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static void ExpandNoteSlot(RoomRelicView slot, RoomNoteData[] notes)
        {
            var settings = new SerializedObject(slot);
            var variants = settings.FindProperty("_variants");
            foreach (var note in notes)
            {
                bool exists = false;
                GameObject template = null;
                for (int i = 0; i < variants.arraySize; i++)
                {
                    var binding = variants.GetArrayElementAtIndex(i);
                    string id = binding.FindPropertyRelative("_id").stringValue;
                    if (id == note.id) exists = true;
                    if (id.EndsWith(note.visualType == RoomNoteVisualType.PaperBall ? "_02" : "_01"))
                        template = (GameObject)binding.FindPropertyRelative("_target").objectReferenceValue;
                }
                if (exists) continue;
                if (template == null) throw new System.InvalidOperationException("Missing note template: " + slot.name);
                var target = Object.Instantiate(template, template.transform.parent);
                target.name = note.id;
                target.SetActive(false);
                Undo.RegisterCreatedObjectUndo(target, "Author note variant");
                int index = variants.arraySize++;
                var added = variants.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("_id").stringValue = note.id;
                added.FindPropertyRelative("_target").objectReferenceValue = target;
            }
            settings.ApplyModifiedProperties();
        }

        private static void Ref(SerializedObject settings, string name, Object value) => settings.FindProperty(name).objectReferenceValue = value;
    }
}
#endif
