#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>通过公寓视口点击家具，打开 Inspector 绑定的现有页面。</summary>
    [DisallowMultipleComponent]
    public sealed class FurniturePageLink : MonoBehaviour
    {
        [SerializeField] private Collider2D? _hitArea;
        [SerializeField] private PanelId _targetPanel;

        public PanelId TargetPanel => _targetPanel;

        public bool TryHandleWorldPoint(Vector2 point)
        {
            if (!isActiveAndEnabled || _hitArea == null || !_hitArea.enabled || !_hitArea.OverlapPoint(point)) return false;
            if (!ClickOcclusionUtility.TryGetTopmostColliderAtWorldPoint(point, out Collider2D? hit) ||
                hit == null || hit.GetComponentInParent<FurniturePageLink>() != this) return false;
            if (!ServiceLocator.TryResolve(out IUIRouter? router) || router == null || router.Top != PanelId.SpaceSys) return false;
            return _targetPanel != PanelId.None && router.Open(_targetPanel);
        }
    }
}
