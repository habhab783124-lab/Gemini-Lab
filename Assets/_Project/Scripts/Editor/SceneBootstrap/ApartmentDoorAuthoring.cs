#if UNITY_EDITOR
using System.Linq;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentDoorAuthoring
    {
        private const string Folder = "Assets/_Project/Settings/IndoorDoor";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/IndoorDoorDialogues.asset";

        [MenuItem("Tools/Gemini-Lab/Apartment/Author Indoor Door")]
        public static void Author()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Apartment/Apartment_Main.unity") return;
            if (Object.FindObjectsByType<ApartmentDoorInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any())
            { Debug.Log("小门已绑定，请在 Inspector 调整，避免覆盖已有作者化内容。"); return; }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Settings", "IndoorDoor");
            var door = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(r => r.gameObject.scene == scene && r.sprite != null && AssetDatabase.GetAssetPath(r.sprite) == "Assets/_Project/Art/补充/Space_sys/door.png");
            var controller = Undo.AddComponent<ApartmentDoorInteraction>(door.gameObject);
            var so = new SerializedObject(controller);
            Ref(so, "_clickArea", door.GetComponent<Collider2D>()); Ref(so, "_closedVisual", door);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/VertexColor.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, Folder + "/VertexColor.mat");
            }
            // 开门形态是同尺寸的五条竖向矩形，可在场景中直接调整；不修改原始带锁图片。
            GameObject open = Child(door.transform, "OpenDoor");
            Color[] colors = { Hex("9EAFC5"), Hex("D7E2F2"), Hex("9EAFC5"), Hex("D7E2F2"), Hex("9EAFC5") };
            Mesh quad = MeshAsset("Quad", new[] { new Vector3(-.5f,-.5f), new Vector3(-.5f,.5f), new Vector3(.5f,.5f), new Vector3(.5f,-.5f) }, new[] {0,1,2,0,2,3}, null);
            for (int i = 0; i < 5; i++)
            {
                GameObject stripe = Child(open.transform, "Stripe" + i);
                stripe.transform.localPosition = new Vector3(-.20f + i * .09f, .03f, 0);
                stripe.transform.localScale = new Vector3(.09f, 3.36f, 1);
                MeshRenderer renderer = MeshObject(stripe, quad, material);
                string path = Folder + "/Stripe" + i + ".mat";
                var stripeMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (stripeMaterial == null) { stripeMaterial = new Material(material) { color = colors[i] }; AssetDatabase.CreateAsset(stripeMaterial, path); }
                renderer.sharedMaterial = stripeMaterial;
                renderer.sortingLayerID = door.sortingLayerID; renderer.sortingOrder = door.sortingOrder;
            }
            open.SetActive(false); Ref(so, "_openVisual", open);
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Folder + "/PetSlide.physicsMaterial2D");
            if (friction == null) { friction = new PhysicsMaterial2D("PetSlide") { friction = 0, bounciness = 0 }; AssetDatabase.CreateAsset(friction, Folder + "/PetSlide.physicsMaterial2D"); }
            foreach (var pet in Object.FindObjectsByType<PetController>(FindObjectsSortMode.None).Where(p => p.gameObject.scene == scene))
            {
                string side = pet.PetId == PetId.Angel ? "angel" : "devil";
                Ref(so, "_" + side, pet);
                var interaction = new SerializedObject(pet.GetComponent<PetPlayerFurnitureInteractionController>());
                var bindings = interaction.FindProperty("_bindings");
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    if (binding.FindPropertyRelative("_label").stringValue == "门边")
                        Ref(so, "_" + side + "Approach", binding.FindPropertyRelative("_approachPoint").objectReferenceValue);
                }
                var click = new SerializedObject(pet.GetComponent<PetClickReactionController>());
                var selection = Child(pet.transform, "SelectionArea").AddComponent<BoxCollider2D>();
                selection.isTrigger = true; selection.size = new Vector2(3.2f,5.5f); selection.offset = new Vector2(0,-.25f);
                Ref(click, "_selectionArea", selection);
                Ref(click, "_controlIndicator", pet.transform.Find("ControlIndicator")?.GetComponent<SpriteRenderer>());
                click.FindProperty("_socializeOnClick").boolValue = false; click.ApplyModifiedProperties();
                var body = pet.GetComponent<Rigidbody2D>(); Undo.RecordObject(body, "Remove movement drag"); body.drag = 0; body.angularDrag = 0;
                var foot = pet.GetComponent<CapsuleCollider2D>(); Undo.RecordObject(foot, "Use frictionless pet footprint"); foot.sharedMaterial = friction;
                var motor = new SerializedObject(pet.GetComponent<ApartmentPetMovement>()); motor.FindProperty("_skin").floatValue = .02f; motor.ApplyModifiedProperties();

            }
            var bridge = Object.FindObjectsByType<ApartmentViewportInputBridge>(FindObjectsSortMode.None).Single(b => b.gameObject.scene == scene);
            var viewport = Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(r=>r.gameObject.scene == scene && r.name == "ApartmentViewportImage");
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.gameObject.scene == scene && c.targetTexture == viewport.texture);
            var bridgeObject = new SerializedObject(bridge); Ref(bridgeObject, "_indoorDoor", controller); Ref(bridgeObject, "_viewportImage", viewport); Ref(bridgeObject, "_viewportCamera", camera); bridgeObject.ApplyModifiedProperties();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai SDF.asset");
            GameObject ui = UIChild(bridge.transform, "IndoorDoorUI", new Vector2(.5f,0), new Vector2(0,0), new Vector2(1160,340));
            var uiRect = (RectTransform)ui.transform; uiRect.pivot = new Vector2(.5f,0);
            TMP_Text hint = Text(ui.transform, "Hint", font, new Vector2(0,22), new Vector2(1120,40), 27); hint.alignment = TextAlignmentOptions.Center; hint.text = "点击小门开门"; hint.color = new Color(.18f,.13f,.17f);
            Ref(so, "_hint", hint);
            Ref(so, "_dialogues", CreateCatalog()); so.ApplyModifiedProperties();
            AuthorSpeechBubbles();
            UpgradePassage();
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[IndoorDoor] 已绑定门、十组对话、选中箭头和低摩擦移动。请保存场景。");
        }

        [MenuItem("Tools/Gemini-Lab/Apartment/Author Door Speech Bubbles")]
        public static void AuthorSpeechBubbles()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Apartment/Apartment_Main.unity") return;
            var door = Object.FindObjectsByType<ApartmentDoorInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene);
            var settings = new SerializedObject(door);
            var angel = (PetController)settings.FindProperty("_angel").objectReferenceValue;
            var devil = (PetController)settings.FindProperty("_devil").objectReferenceValue;
            var bridge = Object.FindObjectsByType<ApartmentViewportInputBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.gameObject.scene == scene);
            var bridgeSettings = new SerializedObject(bridge);
            var viewport = (RawImage)bridgeSettings.FindProperty("_viewportImage").objectReferenceValue;
            var camera = (Camera)bridgeSettings.FindProperty("_viewportCamera").objectReferenceValue;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai SDF.asset");
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            foreach (var pet in new[] { angel, devil })
            {
                string field = pet == angel ? "_angelBubble" : "_devilBubble";
                if (settings.FindProperty(field).objectReferenceValue != null) continue;
                var root = UIChild(viewport.transform, pet == angel ? "AngelDialogueBubble" : "DevilDialogueBubble", new Vector2(.5f,.5f), Vector2.zero, new Vector2(420,230));
                ((RectTransform)root.transform).pivot = new Vector2(.5f,0);
                var bubble = root.AddComponent<PetDialogueBubble>();
                var content = UIChild(root.transform, "Content", new Vector2(.5f,0), new Vector2(0,115), new Vector2(420,230));
                var tail = UIChild(content.transform, "Tail", new Vector2(.5f,0), new Vector2(0,4), new Vector2(24,24));
                tail.transform.localRotation = Quaternion.Euler(0,0,45);
                Color color = pet == angel ? Hex("FFF8E6") : Hex("F6EDFF");
                var tailImage = tail.AddComponent<Image>(); tailImage.color=color; tailImage.raycastTarget=false;
                var body = UIChild(content.transform, "Body", new Vector2(.5f,.5f), Vector2.zero, new Vector2(420,230));
                var background = body.AddComponent<Image>(); background.sprite=rounded; background.type=Image.Type.Sliced; background.color=color; background.raycastTarget=false;
                var text = Text(body.transform, "Text", font, new Vector2(0,115), new Vector2(368,184), 32);
                text.alignment=TextAlignmentOptions.MidlineLeft; text.text="在这里编辑气泡的样式与尺寸。";
                var bubbleSettings = new SerializedObject(bubble);
                Ref(bubbleSettings,"_pet",pet.transform); Ref(bubbleSettings,"_otherPet",pet == angel ? devil.transform : angel.transform);
                Ref(bubbleSettings,"_camera",camera); Ref(bubbleSettings,"_viewport",viewport);
                Ref(bubbleSettings,"_content",content); Ref(bubbleSettings,"_text",text); Ref(bubbleSettings,"_tail",tail.transform);
                bubbleSettings.FindProperty("_offset").vector2Value=new Vector2(pet == angel ? 210 : -210,24);
                bubbleSettings.ApplyModifiedProperties();
                content.SetActive(false); Ref(settings,field,bubble);
            }
            settings.ApplyModifiedProperties();
            // 仅移除旧工具生成的集中式对话，保留门提示与其他已作者化内容。
            var oldPanel = bridge.transform.Find("IndoorDoorUI/Conversation");
            if (oldPanel != null) Undo.DestroyObjectImmediate(oldPanel.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[IndoorDoor] 双宠气泡已绑定；可启用 Content 在 Scene 中预览，修改样式后关闭预览并保存。");
        }

        [MenuItem("Tools/Gemini-Lab/Apartment/Upgrade Door Passage")]
        public static void UpgradePassage()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Apartment/Apartment_Main.unity") return;
            var door = Object.FindObjectsByType<ApartmentDoorInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d=>d.gameObject.scene==scene);
            var settings = new SerializedObject(door);
            var angel = (PetController)settings.FindProperty("_angel").objectReferenceValue;
            var devil = (PetController)settings.FindProperty("_devil").objectReferenceValue;
            var angelMotor = new SerializedObject(angel.GetComponent<ApartmentPetMovement>());
            var devilMotor = new SerializedObject(devil.GetComponent<ApartmentPetMovement>());
            var right = (BoxCollider2D)angelMotor.FindProperty("_room").objectReferenceValue;
            var left = (BoxCollider2D)devilMotor.FindProperty("_room").objectReferenceValue;
            Ref(angelMotor,"_connectedRoom",left); Ref(devilMotor,"_connectedRoom",right);
            angelMotor.ApplyModifiedProperties(); devilMotor.ApplyModifiedProperties();
            var click = (BoxCollider2D)settings.FindProperty("_clickArea").objectReferenceValue;
            Undo.RecordObject(click,"Door click trigger"); click.isTrigger=true;
            Bounds opening=click.bounds;
            float minX=left.bounds.max.x, maxX=right.bounds.min.x;
            float minY=Mathf.Min(left.bounds.min.y,right.bounds.min.y),maxY=Mathf.Max(left.bounds.max.y,right.bounds.max.y);
            BoxCollider2D Wall(string name,float bottom,float top)
            {
                var existing=door.transform.Find(name);
                if(existing!=null) return existing.GetComponent<BoxCollider2D>(); // 保留已手调形状。
                var go=Child(door.transform,name);
                go.transform.position=new Vector3((minX+maxX)*.5f,(bottom+top)*.5f,door.transform.position.z);
                var wall=go.AddComponent<BoxCollider2D>();
                wall.size=new Vector2((maxX-minX)/Mathf.Abs(go.transform.lossyScale.x),(top-bottom)/Mathf.Abs(go.transform.lossyScale.y));
                return wall;
            }
            Wall("DividerLower",minY,opening.min.y);
            Wall("DividerUpper",opening.max.y,maxY);
            var blocker=Wall("PassageBlocker",opening.min.y,opening.max.y);
            Ref(settings,"_passageBlocker",blocker); settings.ApplyModifiedProperties();
            blocker.enabled=!door.IsOpen;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static IndoorDoorDialogueCatalog CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<IndoorDoorDialogueCatalog>(CatalogPath);
            if (catalog != null) return catalog;
            catalog = ScriptableObject.CreateInstance<IndoorDoorDialogueCatalog>();
            string[][] rows = {
                new[]{"吉他响了","你那把吉他刚才自己响了一声，我还以为你在叫我。","我可没叫。……不过你都听见了，就过来坐会儿。","大概是弦松了吧。","嗯，随它响吧……我现在懒得管。"},
                new[]{"苹果滚到床边","你那堆零食里有个苹果滚到床边去了。","那是它自己选的位置。你捡回来，我分你一半。","哦，难怪刚才听见“咚”的一声。","我现在不想起身…"},
                new[]{"扭蛋机里有什么","我每次看你那台扭蛋机，都有点好奇里面到底还藏着什么。","想知道？那你陪我抽一次。出了好东西可别跟我抢。","抽出来不就知道了。","今天先算了……我连转它一下都嫌累。"},
                new[]{"点一首歌","如果我点一首，你会弹吗？","你点吧，今天给你破例。","看难不难，先说名字。","下次吧，我今天不想弹。"},
                new[]{"蝙蝠海报","你很喜欢墙上那只蝙蝠？","还行。你盯久了也挺像它。","挂着顺眼，就一直没摘。","就一张画而已。"},
                new[]{"水晶球与塔罗","你那颗水晶球……真能告诉人今天会遇到什么？","不一定能告诉你答案。不过你想的话，我们可以一起抽一张看看。","它只是帮人换个角度想事情。你也可以试试看。","今天就先不问它了…"},
                new[]{"竖琴","你弹琴时真的不会弹错吗？","会呀。你在的话我更不怕。","会，错了再重新弹就好。","今天不弹了，我想休息一下。"},
                new[]{"书架上的书","你书架上哪本最不无聊？","我挑一本，你留下来一起看呀。","左边第三本，你应该会喜欢。","改天我再帮你找，好吗？"},
                new[]{"植物","这盆植物怎么总朝窗边歪？","它在追光。一起把它转回来？","它会朝亮处长，很正常。","先让它晒着吧，我有点累。"},
                new[]{"窗边阳光","你这里什么时候太阳最好？","下午最好，你可以来晒会儿。","中午最亮，植物也喜欢。","没注意过。"}
            };
            var serialized = new SerializedObject(catalog); var topics = serialized.FindProperty("_topics"); topics.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var topic = topics.GetArrayElementAtIndex(i); topic.FindPropertyRelative("Initiator").intValue = (int)(i < 5 ? PetId.Angel : PetId.Devil);
                topic.FindPropertyRelative("Id").stringValue = (i < 5 ? "angel_" : "devil_") + (i % 5 + 1);
                string[] fields = {"Title","Opening","Warm","Normal","NeedSpace"};
                for (int j = 0; j < fields.Length; j++) topic.FindPropertyRelative(fields[j]).stringValue = rows[i][j];
            }
            serialized.ApplyModifiedProperties(); AssetDatabase.CreateAsset(catalog, CatalogPath); return catalog;
        }

        private static void Ref(SerializedObject target, string field, Object value) => target.FindProperty(field).objectReferenceValue = value;
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
        private static GameObject Child(Transform parent, string name) { var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Author indoor door"); go.transform.SetParent(parent,false); return go; }
        private static Mesh MeshAsset(string name, Vector3[] vertices, int[] triangles, Color[] colors)
        {
            string path = Folder + "/" + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            mesh = new Mesh { name = name, vertices = vertices, triangles = triangles, colors = colors ?? Enumerable.Repeat(Color.white,vertices.Length).ToArray() }; mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        private static MeshRenderer MeshObject(GameObject go, Mesh mesh, Material material) { go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material; return r; }
        private static GameObject UIChild(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        { var go = new GameObject(name,typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go,"Author door UI"); var rt=(RectTransform)go.transform; rt.SetParent(parent,false); rt.anchorMin=rt.anchorMax=anchor; rt.anchoredPosition=position; rt.sizeDelta=size; return go; }
        private static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,Vector2 position,Vector2 size,float fontSize)
        { var go=UIChild(parent,name,new Vector2(.5f,0),position,size); var text=go.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=fontSize; text.color=new Color(.22f,.17f,.20f); text.raycastTarget=false; text.enableWordWrapping=true; text.alignment=TextAlignmentOptions.MidlineLeft; return text; }
    }
}
#endif
