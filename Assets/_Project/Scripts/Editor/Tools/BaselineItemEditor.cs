#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// BaselineItem 的 Inspector 只展示并编辑它所绑定的共享基线定义，
    /// 不复制一套本地 Y、范围或排序参数。
    /// </summary>
    [CustomEditor(typeof(BaselineItem))]
    public sealed class BaselineItemEditor : UnityEditor.Editor
    {
        private SerializedProperty? _baselineDefinition;
        private SerializedProperty? _allowDrag;
        private SerializedProperty? _solidCollider;

        private void OnEnable()
        {
            _baselineDefinition = serializedObject.FindProperty("_baselineDefinition");
            _allowDrag = serializedObject.FindProperty("_allowDrag");
            _solidCollider = serializedObject.FindProperty("_solidCollider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (_baselineDefinition != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_baselineDefinition, new GUIContent("所属基线"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    WorldMapBaselineEditorUtility.RefreshBaselineBinding((BaselineItem)target);
                }
            }

            if (_allowDrag != null) EditorGUILayout.PropertyField(_allowDrag);
            if (_solidCollider != null) EditorGUILayout.PropertyField(_solidCollider);
            serializedObject.ApplyModifiedProperties();

            BaselineItem item = (BaselineItem)target;
            WorldMapBaselineDefinition? definition = item.BaselineDefinition;
            if (definition == null)
            {
                EditorGUILayout.HelpBox(
                    "必须绑定一条 WorldMapBaselineDefinition。基线位置、范围和渲染顺序不在 BaselineItem 中单独保存。",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("共享基线参数", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("ID", definition.Id);
                EditorGUILayout.LabelField("槽位", definition.SlotIndex.ToString());
                EditorGUILayout.LabelField("分组", definition.Group.ToString());
            }

            EditorGUI.BeginChangeCheck();
            float baselineY = EditorGUILayout.FloatField("基线 Y", definition.BaselineY);
            if (EditorGUI.EndChangeCheck())
                WorldMapBaselineEditorUtility.TrySetBaselineY(
                    definition, baselineY, "修改共享基线 Y");

            EditorGUI.BeginChangeCheck();
            int renderOrder = EditorGUILayout.IntField(
                new GUIContent("渲染顺序（相对值）", "直接编辑绑定基线的共享值；只比较大小，数值越大越靠前，不代表基线条数。"),
                definition.RenderOrder);
            if (EditorGUI.EndChangeCheck())
                WorldMapBaselineEditorUtility.TrySetRenderOrder(
                    definition, renderOrder, "修改共享基线渲染顺序");

            SerializedObject definitionSerialized = new SerializedObject(definition);
            definitionSerialized.Update();
            EditorGUI.BeginChangeCheck();
            DrawSharedDefinitionProperty(definitionSerialized, "_displayName", "显示名称");
            DrawSharedDefinitionProperty(definitionSerialized, "_editorColor", "编辑器颜色");
            DrawSharedDefinitionProperty(definitionSerialized, "_allowFlowerPlacement", "允许花朵摆放");
            DrawSharedDefinitionProperty(definitionSerialized, "_xOffset", "X 偏移");
            DrawSharedDefinitionProperty(definitionSerialized, "_minX", "最小 X");
            DrawSharedDefinitionProperty(definitionSerialized, "_maxX", "最大 X");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(definition, "修改共享基线参数");
                definitionSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(definition);
                EditorSceneManager.MarkSceneDirty(definition.gameObject.scene);
            }

            if (GUILayout.Button("在 Scene 中选中这条基线"))
            {
                Selection.activeGameObject = definition.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying || BaselineItem.IsEditorBaselineMoveInProgress) return;

            BaselineItem item = (BaselineItem)target;
            if (!item.HasBaselineDefinition) return;

            float expectedY = item.BaselineY + item.BaselineTransformOffset;
            if (Mathf.Abs(item.transform.position.y - expectedY) <= 0.0001f) return;

            Undo.RecordObject(item.transform, "锁定 BaselineItem Y 轴");
            Vector3 position = item.transform.position;
            position.y = expectedY;
            item.transform.position = position;
            item.transform.hasChanged = false;
            EditorUtility.SetDirty(item.transform);
        }

        private static void DrawSharedDefinitionProperty(
            SerializedObject serialized, string propertyName, string label)
        {
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }
}
#endif
