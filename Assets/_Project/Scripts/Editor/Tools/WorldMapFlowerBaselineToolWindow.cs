#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// Scene 视图中的七条固定基线调整工具。只绘制和编辑场景已有定义，
    /// 不创建运行时对象；拖动基线时绑定的 BaselineItem 一起沿 Y 轴移动。
    /// </summary>
    public sealed class WorldMapFlowerBaselineToolWindow : EditorWindow
    {
        private static WorldMapFlowerBaselineToolWindow? _instance;
        private bool _showHandles = true;

        [MenuItem("Tools/Gemini-Lab/WorldMap/场景基线")]
        private static void Open()
        {
            _instance = GetWindow<WorldMapFlowerBaselineToolWindow>("WorldMap 基线");
            _instance.Show();
        }

        private void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("七条固定基线：蓝、蓝、白、白、红、白、白。拖动线上的手柄可调整高度，同线物体会跟随。", MessageType.Info);
            _showHandles = EditorGUILayout.ToggleLeft("显示可拖拽手柄", _showHandles);
            if (GUILayout.Button("选中 FlowerPlacementGrid"))
            {
                GameObject? grid = GameObject.Find("FlowerPlacementGrid");
                if (grid != null) Selection.activeGameObject = grid;
            }

            foreach (WorldMapBaselineDefinition definition in GetDefinitions())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatch = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
                    EditorGUI.DrawRect(swatch, definition.EditorColor);
                    EditorGUILayout.LabelField($"{definition.SlotIndex + 1}. {definition.DisplayName}",
                        $"Y {definition.BaselineY:0.###}");
                }
            }
            Repaint();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (_instance == null || !_instance._showHandles) return;
            foreach (WorldMapBaselineDefinition definition in GetDefinitions())
            {
                LineRenderer? line = definition.GetComponent<LineRenderer>();
                if (line == null || line.positionCount < 2) continue;
                Vector3 start = line.GetPosition(0);
                Vector3 end = line.GetPosition(1);
                Handles.color = definition.EditorColor;
                Handles.DrawLine(start, end, 3f);
                Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(midpoint, Quaternion.identity);
                if (!EditorGUI.EndChangeCheck()) continue;

                moved.x = midpoint.x;
                moved.z = midpoint.z;
                float deltaY = moved.y - midpoint.y;
                if (Mathf.Abs(deltaY) <= 0.0001f) continue;
                Undo.RecordObject(definition, "移动 WorldMap 基线");
                Undo.RecordObject(line, "移动 WorldMap 基线");
                line.SetPosition(0, new Vector3(start.x, moved.y, start.z));
                line.SetPosition(1, new Vector3(end.x, moved.y, end.z));
                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_baselineY")!.floatValue = moved.y;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MoveBoundItems(definition, deltaY);
                EditorUtility.SetDirty(line);
                EditorSceneManager.MarkSceneDirty(definition.gameObject.scene);
            }
        }

        private static void MoveBoundItems(WorldMapBaselineDefinition definition, float deltaY)
        {
            BaselineItem[] items = Object.FindObjectsByType<BaselineItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                BaselineItem item = items[i];
                if (item.BaselineDefinition != definition) continue;
                Undo.RecordObject(item.transform, "移动基线上的物体");
                Vector3 position = item.transform.position;
                position.y += deltaY;
                item.transform.position = position;
                EditorUtility.SetDirty(item);
            }
        }

        private static List<WorldMapBaselineDefinition> GetDefinitions()
        {
            var definitions = new List<WorldMapBaselineDefinition>();
            WorldMapBaselineDefinition[] all = Object.FindObjectsByType<WorldMapBaselineDefinition>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                WorldMapBaselineDefinition definition = all[i];
                if (definition.transform.parent != null &&
                    definition.transform.parent.name == "FlowerPlacementGrid")
                    definitions.Add(definition);
            }
            definitions.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
            return definitions;
        }
    }
}
#endif
