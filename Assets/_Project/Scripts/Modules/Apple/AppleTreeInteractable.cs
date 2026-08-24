#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 挂在 Scene 已有的大树根节点上。点击等价于一次晃树，领取该树的缓存苹果。
    /// 不创建任何运行时视觉对象，树的 Sprite/Collider 仍由 Scene 作者化。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AppleTreeInteractable : MonoBehaviour
    {
        [SerializeField] private string _treeId = string.Empty;
        [SerializeField] private AppleTreeFeedback? _feedback;
        private Collider2D? _collider;

        public string TreeId => _treeId;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            EnsureService()?.EnsureTree(_treeId);
        }

        private void OnMouseDown()
        {
            if (ClickOcclusionUtility.IsPointerOverUI()) return;
            if (_collider == null || !ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider)) return;

            int collected = EnsureService()?.ShakeTree(_treeId) ?? 0;
            if (collected > 0)
            {
                _feedback?.ShowCollected(collected);
                Debug.Log($"[AppleTree] {_treeId} 晃树领取 {collected} 个苹果");
            }
            else
            {
                _feedback?.ShowNotReady();
                Debug.Log($"[AppleTree] {_treeId} 当前没有可领取的苹果");
            }
        }

        private static IAppleService? EnsureService()
        {
            return ServiceLocator.TryResolve(out IAppleService? service) ? service : null;
        }
    }
}
