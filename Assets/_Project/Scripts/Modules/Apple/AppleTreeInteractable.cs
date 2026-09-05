#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// Scene-authored click entry for an apple tree. The drop controller owns
    /// the visible shake/drop sequence; this component only performs the
    /// occlusion-safe click routing.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AppleTreeInteractable : MonoBehaviour
    {
        [SerializeField] private string _treeId = string.Empty;
        [SerializeField] private AppleTreeFeedback? _feedback;
        [SerializeField] private AppleTreeDropController? _dropController;
        private Collider2D? _collider;

        public string TreeId => _treeId;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (ServiceLocator.TryResolve(out IAppleService? service) && service is not null)
            {
                service.EnsureTree(_treeId);
            }
        }

        private void OnMouseDown()
        {
            if (ClickOcclusionUtility.IsPointerOverUI()) return;
            if (_collider == null || !ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;

            if (_dropController == null)
            {
                Debug.LogError($"[AppleTree] {_treeId} 缺少 AppleTreeDropController，请运行苹果树作者化。", this);
                _feedback?.ShowNotReady();
                return;
            }

            _dropController.TryBeginHarvest();
        }
    }
}
