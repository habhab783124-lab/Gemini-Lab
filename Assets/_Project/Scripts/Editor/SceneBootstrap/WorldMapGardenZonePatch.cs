#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 WorldMap 场景中创建情绪花园双入口（天使 + 恶魔）+ 共享九宫格花圃可视化。
    /// 幂等：已存在的入口/组件会复用。
    /// </summary>
    public static class WorldMapGardenZonePatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const float GroundY = -3f;

        private static readonly string[] AngelSignNames =
        {
            "天使标牌", "天使区域标牌", "天使区域的标牌", "标牌_天使", "AngelSign", "AngelSignboard"
        };

        private static readonly string[] DemonSignNames =
        {
            "恶魔标牌", "恶魔区域标牌", "恶魔区域的标牌", "标牌_恶魔", "DevilSign", "DevilSignboard"
        };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("_SceneRoot");
            if (root == null)
            {
                Debug.LogError("[WorldMapGardenZonePatch] _SceneRoot 不存在");
                return;
            }

            var parent = root.transform;

            // 1) 清理旧的单一大花园（如果有，移走 WorldMapGardenZone 避免冲突）
            RemoveOldGardenZoneEntry(parent);

            // 2) 双入口：直接使用场景中已有的天使/恶魔标牌，不再创建占位入口。
            EnsureSignEntry(AngelSignNames, "angel");
            EnsureSignEntry(DemonSignNames, "demon");
            DeactivateLegacyEmotionEntries(parent);

            // 3) 共享九宫格花圃可视化（不带 WorldMapGardenZone）
            EnsureGardenPlots(parent);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapGardenZonePatch] 天使+恶魔双入口 + 九宫格花圃已就绪");
        }

        private static void EnsureSignEntry(string[] candidateNames, string owner)
        {
            GameObject? sign = null;
            for (int i = 0; i < candidateNames.Length; i++)
            {
                sign = GameObject.Find(candidateNames[i]);
                if (sign != null) break;
            }

            if (sign == null)
            {
                Debug.LogWarning($"[WorldMapGardenZonePatch] 未找到{owner}标牌，未创建占位入口");
                return;
            }

            WorldMapGardenZone zone = sign.GetComponent<WorldMapGardenZone>() ?? sign.AddComponent<WorldMapGardenZone>();
            SerializedObject zoneSo = new SerializedObject(zone);
            SerializedProperty? ownerProp = zoneSo.FindProperty("_owner");
            if (ownerProp != null) ownerProp.stringValue = owner;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }

        private static void DeactivateLegacyEmotionEntries(Transform parent)
        {
            foreach (string name in new[] { "EmotionEntry_Angel", "EmotionEntry_Demon" })
            {
                Transform? legacy = parent.Find(name);
                if (legacy != null) legacy.gameObject.SetActive(false);
            }
        }

        /// <summary>彻底删除旧版 GardenZone（单入口时代遗留），避免和新双入口 + GardenPlots 混淆。</summary>
        private static void RemoveOldGardenZoneEntry(Transform parent)
        {
            var old = parent.Find("GardenZone");
            if (old == null) return;

            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[WorldMapGardenZonePatch] 已移除旧版 GardenZone");
        }

        private static void EnsureEmotionEntry(Transform parent, string name, string owner, string label,
            float defaultX, float defaultY)
        {
            var existing = parent.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                // 不覆盖 transform / renderer / collider，用户可能已手动调整
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(defaultX, defaultY, 0);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(3f, 3f);
                sr.sortingLayerName = "Furniture";
                sr.sortingOrder = 4;
                sr.color = owner == "angel"
                    ? new Color(0.95f, 0.85f, 0.45f, 0.95f)
                    : new Color(0.7f, 0.18f, 0.25f, 0.95f);

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(3f, 3f);
                col.isTrigger = true;
            }

            // 始终补缺失组件和连线
            var zone = go.GetComponent<WorldMapGardenZone>();
            if (zone == null) zone = go.AddComponent<WorldMapGardenZone>();
            var zoneSo = new SerializedObject(zone);
            var ownerProp = zoneSo.FindProperty("_owner");
            if (ownerProp != null && ownerProp.stringValue != owner)
            {
                ownerProp.stringValue = owner;
                zoneSo.ApplyModifiedProperties();
            }

            var srExist = go.GetComponent<SpriteRenderer>();
            if (srExist == null)
            {
                srExist = go.AddComponent<SpriteRenderer>();
                srExist.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                srExist.drawMode = SpriteDrawMode.Sliced;
                srExist.size = new Vector2(3f, 3f);
                srExist.sortingLayerName = "Furniture";
                srExist.sortingOrder = 4;
            }

            var colExist = go.GetComponent<BoxCollider2D>();
            if (colExist == null)
            {
                colExist = go.AddComponent<BoxCollider2D>();
                colExist.size = new Vector2(3f, 3f);
                colExist.isTrigger = true;
            }

            // Label
            var labelT = go.transform.Find("Label");
            if (labelT == null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                labelGo.transform.localPosition = new Vector3(0, 1.7f, 0);
                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = label;
                tmp.fontSize = 3;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.sortingOrder = 11;
            }
        }

        /// <summary>共享九宫格花圃可视化（纯视觉，不挂 WorldMapGardenZone）。</summary>
        private static void EnsureGardenPlots(Transform parent)
        {
            const string name = "GardenPlots";
            var existing = parent.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                // 不覆盖 transform，用户可能已手动调整位置
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0, GroundY + 1.2f, 0);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color = new Color(0.35f, 0.28f, 0.18f, 0.7f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(5.5f, 5.5f);
                sr.sortingLayerName = "Furniture";
                sr.sortingOrder = 4;

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(5.5f, 5.5f);
                col.isTrigger = true;
            }

            // 只补缺失组件，不动已有属性
            var srExist = go.GetComponent<SpriteRenderer>();
            if (srExist == null)
            {
                srExist = go.AddComponent<SpriteRenderer>();
                srExist.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                srExist.drawMode = SpriteDrawMode.Sliced;
                srExist.size = new Vector2(5.5f, 5.5f);
                srExist.sortingLayerName = "Furniture";
                srExist.sortingOrder = 4;
            }

            var colExist = go.GetComponent<BoxCollider2D>();
            if (colExist == null)
            {
                colExist = go.AddComponent<BoxCollider2D>();
                colExist.size = new Vector2(5.5f, 5.5f);
                colExist.isTrigger = true;
            }

            var plotView = go.GetComponent<GardenPlotView>();
            if (plotView == null)
            {
                plotView = go.AddComponent<GardenPlotView>();
                var pvSo = new SerializedObject(plotView);
                pvSo.FindProperty("_cellSize").floatValue = 1.2f;
                pvSo.FindProperty("_cellGap").floatValue = 0.18f;
                pvSo.ApplyModifiedProperties();
            }

            if (go.transform.Find("Label") == null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                labelGo.transform.localPosition = new Vector3(0, 3.2f, 0);
                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = "九宫格花圃";
                tmp.fontSize = 3;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.sortingOrder = 11;
            }

            // 预建 9 个占位格子，Edit 模式下也能看到花圃结构
            EnsurePlotCells(go.transform);
        }

        private static void EnsurePlotCells(Transform plotsTransform)
        {
            const float cellSize = 1.2f;
            const float cellGap = 0.18f;
            var cellTotal = cellSize + cellGap;
            var startX = -(cellTotal * 2f) / 2f;

            for (int i = 0; i < 9; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x = startX + col * cellTotal;
                float y = -row * cellTotal;

                var cellName = $"Plot_{i}";
                var existing = plotsTransform.Find(cellName);
                if (existing != null) continue;

                var cellGo = new GameObject(cellName);
                cellGo.transform.SetParent(plotsTransform, false);
                cellGo.transform.localPosition = new Vector3(x, y, 0);

                var sr = cellGo.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(cellSize, cellSize);
                sr.sortingLayerName = "Furniture";
                sr.sortingOrder = 5;
                sr.color = new Color(0.35f, 0.3f, 0.25f, 0.7f);
            }
        }
    }
}
#endif
