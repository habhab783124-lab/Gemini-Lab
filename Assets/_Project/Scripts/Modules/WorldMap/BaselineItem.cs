#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 基准线物品：锁死 Y 坐标在绑定的 WorldMapBaselineDefinition 上，仅允许水平拖动。
    /// 基线的 Y、X 范围和渲染顺序全部来自同一个场景定义，确保同一基准线上的物体深度一致。
    /// 挂到场景中每个基准线上的可移动物体（花圃装饰、邮箱、草丛等）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BaselineItem : MonoBehaviour
    {
        [Header("基准线（唯一共享参数源）")]
        [SerializeField] private WorldMapBaselineDefinition? _baselineDefinition;

        [Header("拖拽")]
        [SerializeField] private bool _allowDrag;

        [Header("物理")]
        [SerializeField] private bool _solidCollider;

        private SpriteRenderer? _sprite;
        private Collider2D? _collider;
        private Vector3 _dragOffset;
        private bool _isDragging;
        // 物体轴心与基线的相对偏移作为兼容/保护参数保留；
        // 现有 PSD 场景继续使用各物体原本的世界坐标相对位置。
        [SerializeField, HideInInspector] private float _baselineTransformOffset;

#if UNITY_EDITOR
        /// <summary>
        /// 基线工具批量移动物体时暂时放行 Y 轴同步，普通物体编辑不应设置此标记。
        /// </summary>
        public static bool IsEditorBaselineMoveInProgress { get; set; }
#endif

        public WorldMapBaselineDefinition? BaselineDefinition => _baselineDefinition;
        public bool HasBaselineDefinition => _baselineDefinition != null;
        public float BaselineY => _baselineDefinition!.BaselineY;
        public float EffectiveBaselineY => BaselineY;
        public float MinX => _baselineDefinition!.MinX;
        public float MaxX => _baselineDefinition!.MaxX;
        public int SortingOrder => _baselineDefinition!.SortingOrder;
        public float BaselineTransformOffset => _baselineTransformOffset;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null && !_solidCollider) _collider.isTrigger = true;
            if (_baselineDefinition == null)
            {
                Debug.LogError($"[BaselineItem] {name} 未绑定 WorldMapBaselineDefinition，无法参与基线移动和排序。", this);
                return;
            }
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
                Sprite.sortingOrder = WorldMapBaselineDefinition.ToRendererSortingOrder(SortingOrder);
            }
        }

        /// <summary>
        /// 供 Scene 基线工具在修改基线渲染优先级后刷新已存在的场景物体。
        /// 只更新现有 SpriteRenderer，不创建运行时视觉节点。
        /// </summary>
        public void RefreshSortingOrder()
        {
            if (_baselineDefinition == null) return;
            ApplySortingOrder();
        }

        /// <summary>
        /// 绑定定义变化后将物体重新对齐到共享基线，同时保留当前世界坐标相对偏移。
        /// </summary>
        public void RefreshBaselineBinding()
        {
            if (_baselineDefinition == null) return;
            // BaselineItem may be parented under the PSD background.  The
            // baseline is a world-space line, so preserve the current world
            // position when a binding changes instead of interpreting the
            // parent's serialized local Y as a world Y value.
            _baselineTransformOffset = transform.position.y - BaselineY;
            AlignToBaselineY();
            ApplySortingOrder();
        }

        private void AlignToBaselineY()
        {
            if (_baselineDefinition == null) return;
            Vector3 position = transform.position;
            position.y = BaselineY + _baselineTransformOffset;
            transform.position = position;
        }

        private void OnMouseDown()
        {
            if (!_allowDrag || _baselineDefinition == null) return;
            if (ClickOcclusionUtility.IsPointerOverUI()) return;
            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;
            _dragOffset = transform.position - GetMouseWorldPoint();
            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_allowDrag || !_isDragging || _baselineDefinition == null) return;
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
            if (_baselineDefinition == null) return;
            if (!IsEditorBaselineMoveInProgress) AlignToBaselineY();
            ApplySortingOrder();
        }
#endif
    }
}
