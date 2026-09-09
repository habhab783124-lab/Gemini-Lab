#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 小木屋返回公寓入口。挂到 Cabin 场景物上：
    /// - 点击 → 返回公寓
    /// - 鼠标悬停 → 高亮变色
    /// - 鼠标离开 → 恢复原色
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CabinReturnPortal : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private Color _hoverTint = new Color(1.15f, 1.15f, 1.15f, 1f);
        [SerializeField] private int _interactionPriority = 25;

        private SpriteRenderer? _sprite;
        private Collider2D? _clickCollider;
        private Color _originalColor;

        public bool IsWorldMapInteractionEnabled =>
            isActiveAndEnabled && _clickCollider != null && _clickCollider.enabled;

        public int WorldMapInteractionPriority => _interactionPriority;

        public Renderer? WorldMapSortingRenderer => _sprite;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _clickCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            if (_sprite != null) _originalColor = _sprite.color;
        }

        private void OnMouseEnter()
        {
            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_clickCollider))
            {
                return;
            }

            if (_sprite != null) _sprite.color = _originalColor * _hoverTint;
        }

        private void OnMouseExit()
        {
            if (_sprite != null) _sprite.color = _originalColor;
        }

        private void OnMouseDown()
        {
            if (WorldMapSceneInteractionRouter.Active is { } router && router.IsRegistered(this))
            {
                return;
            }

            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_clickCollider))
            {
                return;
            }

            // 防止 Play 模式启动时 Unity SendMouseEvents 的首帧伪点击
            if (Time.frameCount < 2) return;

            HandleWorldMapClick();
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            _clickCollider ??= GetComponent<Collider2D>();
            return IsWorldMapInteractionEnabled && _clickCollider!.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow) || sceneFlow is null)
            {
                Debug.LogError("[CabinReturnPortal] 未找到 ISceneFlowService", this);
                return;
            }

            sceneFlow.LoadAsync(SceneId.Apartment);
        }
    }
}
