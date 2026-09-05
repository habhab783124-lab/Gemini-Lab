#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// Scene 视图中的固定基线调整工具。只绘制和编辑场景已有定义，
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
            EditorGUILayout.HelpBox("固定基线包含天空、星星、云、树木、地面、花丛和桌宠。拖动线上的手柄可调整高度，同线物体会跟随。", MessageType.Info);
            EditorGUILayout.HelpBox("列表按渲染顺序从前到后显示，RenderOrder 数值越大越靠前；数值只用于相对比较，不代表基线条数。Inspector 与窗口编辑的是同一份基线定义，修改会刷新该基线上的所有物体。", MessageType.None);
            _showHandles = EditorGUILayout.ToggleLeft("显示可拖拽手柄", _showHandles);
            if (GUILayout.Button("选中 FlowerPlacementGrid"))
            {
                GameObject? grid = GameObject.Find("FlowerPlacementGrid");
                if (grid != null) Selection.activeGameObject = grid;
            }

            int displayIndex = 0;
            foreach (WorldMapBaselineDefinition definition in WorldMapBaselineEditorUtility.GetDefinitions())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatch = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
                    EditorGUI.DrawRect(swatch, definition.EditorColor);
                    EditorGUILayout.LabelField($"{displayIndex + 1}. {definition.DisplayName}",
                        GUILayout.MinWidth(130f));
                    EditorGUILayout.LabelField($"Y {definition.BaselineY:0.###}",
                        GUILayout.Width(82f));
                    EditorGUI.BeginChangeCheck();
                    int renderOrder = EditorGUILayout.IntField(
                        new GUIContent("相对顺序", "只比较数值大小，数值越大越靠前，不代表基线条数。"),
                        definition.RenderOrder, GUILayout.Width(102f));
                    if (EditorGUI.EndChangeCheck())
                        WorldMapBaselineEditorUtility.TrySetRenderOrder(
                            definition, renderOrder, "修改基线渲染顺序");
                    GUIStyle moveButtonStyle = EditorStyles.miniButton;
                    if (GUILayout.Button(new GUIContent("前移 ↑", "点击将该基线移到相邻的更靠前层。"),
                        moveButtonStyle, GUILayout.Width(60f), GUILayout.Height(22f)))
                        WorldMapBaselineEditorUtility.TryMoveRenderOrder(
                            definition, 1, "调整基线渲染顺序");
                    if (GUILayout.Button(new GUIContent("后移 ↓", "点击将该基线移到相邻的更靠后层。"),
                        moveButtonStyle, GUILayout.Width(60f), GUILayout.Height(22f)))
                        WorldMapBaselineEditorUtility.TryMoveRenderOrder(
                            definition, -1, "调整基线渲染顺序");
                }
                displayIndex++;
            }
            Repaint();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (_instance == null || !_instance._showHandles) return;
            foreach (WorldMapBaselineDefinition definition in WorldMapBaselineEditorUtility.GetDefinitions())
            {
                LineRenderer? line = definition.GetComponent<LineRenderer>();
                if (line == null || line.positionCount < 2) continue;
                Vector3 start = line.GetPosition(0);
                Vector3 end = line.GetPosition(1);
                Handles.color = definition.EditorColor;
                Handles.DrawLine(start, end, 3f);
                Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
                Handles.Label(midpoint + Vector3.up * 0.08f,
                    $"{definition.DisplayName}  顺序 {definition.RenderOrder}");
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(midpoint, Quaternion.identity);
                if (!EditorGUI.EndChangeCheck()) continue;

                moved.x = midpoint.x;
                moved.z = midpoint.z;
                float deltaY = moved.y - midpoint.y;
                if (Mathf.Abs(deltaY) <= 0.0001f) continue;
                WorldMapBaselineEditorUtility.TrySetBaselineY(
                    definition, moved.y, "移动 WorldMap 基线");
            }
        }
    }
}
#endif
