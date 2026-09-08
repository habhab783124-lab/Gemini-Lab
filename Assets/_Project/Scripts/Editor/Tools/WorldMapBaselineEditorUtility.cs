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
    /// WorldMap 基线的唯一编辑入口。所有 Inspector 和窗口操作都通过这里修改
    /// WorldMapBaselineDefinition，并同步现有的线、绑定物体和 SpriteRenderer。
    /// </summary>
    public static class WorldMapBaselineEditorUtility
    {
        public static List<WorldMapBaselineDefinition> GetDefinitions()
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

            // 工具列表直接反映实际遮挡顺序：RenderOrder 越大越靠前。
            // SlotIndex 只用于相同相对值时提供稳定的显示顺序，不代表渲染层数量。
            definitions.Sort((left, right) =>
            {
                int orderCompare = right.RenderOrder.CompareTo(left.RenderOrder);
                return orderCompare != 0
                    ? orderCompare
                    : left.SlotIndex.CompareTo(right.SlotIndex);
            });
            return definitions;
        }

        public static bool TrySetBaselineY(
            WorldMapBaselineDefinition definition, float baselineY, string undoName)
        {
            float oldY = definition.BaselineY;
            if (Mathf.Abs(oldY - baselineY) <= 0.0001f) return false;

            Undo.RecordObject(definition, undoName);
            LineRenderer? line = definition.GetComponent<LineRenderer>();
            if (line != null && line.positionCount >= 2)
            {
                Undo.RecordObject(line, undoName);
                Vector3 start = line.GetPosition(0);
                Vector3 end = line.GetPosition(1);
                line.SetPosition(0, new Vector3(start.x, baselineY, start.z));
                line.SetPosition(1, new Vector3(end.x, baselineY, end.z));
                EditorUtility.SetDirty(line);
            }

            WriteFloat(definition, "_baselineY", baselineY);
            BaselineItem.IsEditorBaselineMoveInProgress = true;
            try
            {
                MoveBoundItems(definition, baselineY, undoName);
            }
            finally
            {
                BaselineItem.IsEditorBaselineMoveInProgress = false;
            }
            EditorUtility.SetDirty(definition);
            EditorSceneManager.MarkSceneDirty(definition.gameObject.scene);
            return true;
        }

        public static bool TrySetRenderOrder(
            WorldMapBaselineDefinition definition, int renderOrder, string undoName)
        {
            // RenderOrder 是相对值，保留用户输入本身；它不是基线数量或数组下标。
            if (definition.RenderOrder == renderOrder) return false;

            Undo.RecordObject(definition, undoName);
            WriteInt(definition, "_renderOrder", renderOrder);
            RefreshBoundItems(definition, undoName);
            EditorUtility.SetDirty(definition);
            EditorSceneManager.MarkSceneDirty(definition.gameObject.scene);
            return true;
        }

        public static bool TryMoveRenderOrder(
            WorldMapBaselineDefinition definition, int direction, string undoName)
        {
            if (direction == 0) return false;
            List<WorldMapBaselineDefinition> ordered = GetDefinitions();

            int currentIndex = ordered.IndexOf(definition);
            // 列表是从前到后（大到小），所以前移是向列表顶部移动。
            int targetIndex = currentIndex + (direction > 0 ? -1 : 1);
            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
                return false;

            WorldMapBaselineDefinition other = ordered[targetIndex];
            int currentOrder = definition.RenderOrder;
            int otherOrder = other.RenderOrder;
            Undo.RecordObjects(new Object[] { definition, other }, undoName);
            WriteInt(definition, "_renderOrder", otherOrder);
            WriteInt(other, "_renderOrder", currentOrder);
            RefreshBoundItems(definition, undoName);
            RefreshBoundItems(other, undoName);
            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(other);
            EditorSceneManager.MarkSceneDirty(definition.gameObject.scene);
            return true;
        }

        public static void RefreshBaselineBinding(BaselineItem item)
        {
            item.RefreshBaselineBinding();
            EditorUtility.SetDirty(item);
            if (item.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(item.gameObject.scene);
        }

        private static void MoveBoundItems(
            WorldMapBaselineDefinition definition, float baselineY, string undoName)
        {
            BaselineItem[] items = Object.FindObjectsByType<BaselineItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                BaselineItem item = items[i];
                if (item.BaselineDefinition != definition) continue;
                Undo.RecordObject(item.transform, undoName);
                Vector3 position = item.transform.position;
                position.y = baselineY + item.BaselineTransformOffset;
                item.transform.position = position;
                EditorUtility.SetDirty(item);
            }
        }

        private static void RefreshBoundItems(
            WorldMapBaselineDefinition definition, string undoName)
        {
            BaselineItem[] items = Object.FindObjectsByType<BaselineItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                BaselineItem item = items[i];
                if (item.BaselineDefinition != definition) continue;
                SpriteRenderer? sprite = item.GetComponent<SpriteRenderer>();
                if (sprite != null) Undo.RecordObject(sprite, undoName);
                item.RefreshSortingOrder();
                EditorUtility.SetDirty(item);
                if (sprite != null) EditorUtility.SetDirty(sprite);
            }
        }

        private static void WriteFloat(
            WorldMapBaselineDefinition definition, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteInt(
            WorldMapBaselineDefinition definition, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
