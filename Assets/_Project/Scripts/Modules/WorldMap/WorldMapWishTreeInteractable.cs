#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 许愿树的显式点击入口。该物体只打开许愿系统，不执行苹果领取逻辑。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WorldMapWishTreeInteractable : MonoBehaviour
    {
        [SerializeField] private WorldMapWishSystemController? _wishSystem;

        private Collider2D? _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void OnMouseDown()
        {
            if (_collider == null || ClickOcclusionUtility.IsPointerOverUI()) return;
            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;
            _wishSystem?.OpenPanel();
        }

        public void Bind(WorldMapWishSystemController controller)
        {
            _wishSystem = controller;
        }
    }
}
