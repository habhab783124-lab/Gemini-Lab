#if UNITY_EDITOR
using System.Linq;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.HubUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentPageFurnitureAuthoring
    {
        [MenuItem("Tools/Gemini-Lab/Apartment/Author Page Furniture")]
        public static void Author()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/_Project/Scenes/Apartment/Apartment_Main.unity") return;
            Create("CrystalBall", "水晶球", "crystal_ball", PanelId.Tarot, new Vector2(8.5f,2.3f),2.2f,1.2f);
            Create("Gacha", "扭蛋机", "gacha", PanelId.Collection,new Vector2(-11.2f,-.4f),3f,1.4f);
            var bridge=Object.FindObjectsByType<ApartmentViewportInputBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.gameObject.scene==scene);
            var bindings=new SerializedObject(bridge);var links=bindings.FindProperty("_furniturePageLinks");
            var instances=Object.FindObjectsByType<FurniturePageLink>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.gameObject.scene==scene).ToArray();
            links.arraySize=instances.Length;
            for(int i=0;i<instances.Length;i++) links.GetArrayElementAtIndex(i).objectReferenceValue=instances[i];
            bindings.ApplyModifiedProperties();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void Create(string id,string label,string image,PanelId page,Vector2 position,float height,float baseWidth)
        {
            string definitionId="Furniture."+id;
            if(Object.FindObjectsByType<Furniture>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(f=>f.DefinitionId==definitionId)) return;
            string prefabPath="Assets/_Project/Prefabs/Furniture/Leisure/"+id+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab!=null)
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(instance,"Place page furniture");instance.transform.position=position;return;
            }
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Furniture/"+image+".png");
            if(sprite==null) throw new System.InvalidOperationException("Missing furniture sprite: "+image);
            string definitionPath="Assets/_Project/ScriptableObjects/FurnitureConfig/Leisure/"+id+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<FurnitureDefinitionSO>(definitionPath);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<FurnitureDefinitionSO>();var data=new SerializedObject(definition);
                data.FindProperty("_id").stringValue=definitionId;data.FindProperty("_sprite").objectReferenceValue=sprite;
                data.FindProperty("_category").intValue=(int)FurnitureCategory.Leisure;
                data.FindProperty("_placementType").intValue=(int)FurniturePlacementType.Floor;
                data.FindProperty("_occupiedCells").vector2IntValue=new Vector2Int(2,1);
                data.ApplyModifiedProperties();AssetDatabase.CreateAsset(definition,definitionPath);
            }
            var go=new GameObject(label);Undo.RegisterCreatedObjectUndo(go,"Author page furniture");go.transform.position=position;
            float scale=height/sprite.bounds.size.y;go.transform.localScale=Vector3.one*scale;
            var group=go.AddComponent<SortingGroup>();group.sortingOrder=150; // 位于地板上方、可点击遗留物下方。
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=0;
            var foot=go.AddComponent<BoxCollider2D>();foot.size=new Vector2(baseWidth/scale,.45f/scale);
            foot.offset=new Vector2(sprite.bounds.center.x,sprite.bounds.min.y+.225f/scale);
            var anchor=go.AddComponent<InteractionAnchor>();anchor.SetAvailable(false);
            var furniture=go.AddComponent<Furniture>();var settings=new SerializedObject(furniture);
            settings.FindProperty("_definition").objectReferenceValue=definition;settings.FindProperty("_anchor").objectReferenceValue=anchor;settings.FindProperty("_isSceneFurniture").boolValue=true;settings.ApplyModifiedProperties();
            var hitObject=new GameObject("ClickArea");hitObject.transform.SetParent(go.transform,false);
            var hit=hitObject.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=new Vector2(sprite.bounds.size.x*.78f,sprite.bounds.size.y*.96f);hit.offset=sprite.bounds.center;
            var link=go.AddComponent<FurniturePageLink>();var action=new SerializedObject(link);
            action.FindProperty("_hitArea").objectReferenceValue=hit;action.FindProperty("_targetPanel").intValue=(int)page;action.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAssetAndConnect(go,prefabPath,InteractionMode.AutomatedAction);
        }
    }
}
#endif
