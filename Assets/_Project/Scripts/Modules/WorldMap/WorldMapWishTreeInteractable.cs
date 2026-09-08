#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 许愿树的显式点击入口。该物体只打开许愿系统，不执行苹果领取逻辑。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WorldMapWishTreeInteractable : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private WorldMapWishSystemController? _wishSystem;
        [SerializeField] private int _interactionPriority = 15;

        private Collider2D? _collider;
        private Renderer? _renderer;

        public bool IsWorldMapInteractionEnabled => isActiveAndEnabled && _collider != null && _collider.enabled;
        public int WorldMapInteractionPriority => _interactionPriority;
        public Renderer? WorldMapSortingRenderer => _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            return IsWorldMapInteractionEnabled && _collider != null && _collider.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            _wishSystem?.OpenPanel();
        }

        private void OnMouseDown()
        {
            if (WorldMapSceneInteractionRouter.Active?.IsRegistered(this) == true) return;
            if (_collider == null || ClickOcclusionUtility.IsPointerOverUI()) return;
            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;
            HandleWorldMapClick();
        }

        public void Bind(WorldMapWishSystemController controller)
        {
            _wishSystem = controller;
        }
    }
}
