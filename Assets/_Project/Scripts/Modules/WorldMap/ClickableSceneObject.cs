#nullable enable
using GeminiLab.Core;
using UnityEngine;
using UnityEngine.Events;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 可点击的场景交互入口。
    /// 点击通过场景中作者化的序列化 UnityEvent 接入具体业务。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ClickableSceneObject : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private string _displayName = "场景物";
        [SerializeField] private string _clickMessage = "点击了 {0}";
        [SerializeField] private UnityEvent _onClicked = new();
        [SerializeField] private int _interactionPriority;

        /// <summary>
        /// Editor authoring surface for explicit scene click bindings.
        /// </summary>
        public UnityEvent OnClicked => _onClicked;

        public bool IsWorldMapInteractionEnabled => isActiveAndEnabled;

        public int WorldMapInteractionPriority => _interactionPriority;

        public Renderer? WorldMapSortingRenderer => GetComponent<SpriteRenderer>();

        private Collider2D? _clickCollider;

        private void Awake()
        {
            _clickCollider = GetComponent<Collider2D>();
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

            HandleWorldMapClick();
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            _clickCollider ??= GetComponent<Collider2D>();
            return _clickCollider != null && _clickCollider.enabled && _clickCollider.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            Debug.Log($"[ClickableSceneObject] {string.Format(_clickMessage, _displayName)}");
            _onClicked.Invoke();
        }
    }
}
