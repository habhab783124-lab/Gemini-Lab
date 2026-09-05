#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 场景中独立存在的固定基线。基线数量、顺序和颜色由场景节点决定，
    /// 不会因为某个 BaselineItem 被删除而消失。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapBaselineDefinition : MonoBehaviour
    {
        public enum BaselineGroup
        {
            Environment,
            Flower,
            Character
        }

        [SerializeField] private string _id = string.Empty;
        [SerializeField] private int _slotIndex = -1;
        [Tooltip("基线渲染优先级相对值；只比较大小，数值越大越靠前。所有绑定物体和花朵放置层共享此值，不代表基线条数。")]
        [SerializeField] private int _renderOrder;
        [SerializeField] private BaselineGroup _group;
        [SerializeField] private Color _editorColor = Color.white;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private bool _allowFlowerPlacement;
        [SerializeField] private float _xOffset;
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;
        [SerializeField] private float _baselineY;

        public string Id => _id;
        public int SlotIndex => _slotIndex;
        public int RenderOrder => _renderOrder;
        public BaselineGroup Group => _group;
        public Color EditorColor => _editorColor;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;
        public bool AllowFlowerPlacement => _allowFlowerPlacement;
        public float XOffset => _xOffset;
        public float MinX => _minX;
        public float MaxX => _maxX;
        public float BaselineY => _baselineY;
        public bool IsFixedSlot => _slotIndex >= 0;
        // 兼容现有调用方；新的单一事实源是 RenderOrder，它只参与相对大小比较。
        public int SortingOrder => RenderOrder;

        public static int ToRendererSortingOrder(int renderOrder)
        {
            long resolved = 1000L + renderOrder * 1000L;
            if (resolved > int.MaxValue) return int.MaxValue;
            if (resolved < int.MinValue) return int.MinValue;
            return (int)resolved;
        }
    }
}
