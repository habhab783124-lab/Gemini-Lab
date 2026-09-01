#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 基准线物品：锁死 Y 坐标在指定基准线上，仅允许水平拖动，X 受限在 [minX, maxX] 范围内。
    /// 自动管理 SpriteRenderer.sortingOrder，确保同一基准线上的物体深度一致。
    /// 挂到场景中每个基准线上的可移动物体（花圃装饰、邮箱、草丛等）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BaselineItem : MonoBehaviour
    {
        [Header("基准线")]
        [SerializeField] private float _baselineY;
        [SerializeField] private WorldMapBaselineDefinition? _baselineDefinition;

        [Header("X 移动范围")]
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;

        [Header("深度排序")]
        [SerializeField] private int _sortingOrder;

        [Header("拖拽")]
        [SerializeField] private bool _allowDrag;

        [Header("物理")]
        [SerializeField] private bool _solidCollider;

        private SpriteRenderer? _sprite;
        private Collider2D? _collider;
        private Vector3 _dragOffset;
        private bool _isDragging;
        private float _baselineTransformOffset;

        public WorldMapBaselineDefinition? BaselineDefinition => _baselineDefinition;
        public float BaselineY => _baselineDefinition != null ? _baselineDefinition.BaselineY : _baselineY;
        public float EffectiveBaselineY => Mathf.Abs(BaselineY) > 0.0001f ? BaselineY : transform.position.y;
        public float MinX => _baselineDefinition != null ? _baselineDefinition.MinX : _minX;
        public float MaxX => _baselineDefinition != null ? _baselineDefinition.MaxX : _maxX;
        public int SortingOrder => _baselineDefinition != null ? _baselineDefinition.SortingOrder : _sortingOrder;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null && !_solidCollider) _collider.isTrigger = true;
            _baselineTransformOffset = _baselineDefinition != null
                ? transform.position.y - BaselineY
                : 0f;
            ApplySortingOrder();
        }

        private SpriteRenderer Sprite
        {
            get
            {
                if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
                return _sprite;
            }
        }

        private void ApplySortingOrder()
        {
            if (Sprite != null)
            {
                Sprite.sortingOrder = _baselineDefinition != null
                    ? WorldMapBaselineDefinition.ToRendererSortingOrder(_baselineDefinition.SlotIndex)
                    : _sortingOrder;
            }
        }

        private void OnMouseDown()
        {
            if (!_allowDrag) return;
            if (ClickOcclusionUtility.IsPointerOverUI()) return;
            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;
            _dragOffset = transform.position - GetMouseWorldPoint();
            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_allowDrag || !_isDragging) return;
            Vector3 target = GetMouseWorldPoint() + _dragOffset;
            target.y = BaselineY + _baselineTransformOffset;
            target.z = transform.position.z;
            target.x = Mathf.Clamp(target.x, MinX, MaxX);
            transform.position = target;
        }

        private void OnMouseUp()
        {
            _isDragging = false;
        }

        private static Vector3 GetMouseWorldPoint()
        {
            Vector3 screen = Input.mousePosition;
            screen.z = -Camera.main!.transform.position.z;
            return Camera.main.ScreenToWorldPoint(screen);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            ApplySortingOrder();
        }
#endif
    }
}
