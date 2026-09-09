#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 情绪花园种植入口。点击后打开情绪输入面板，owner 由场景中配置决定。
    /// 挂到天使/恶魔各自的入口 GameObject 上，_owner 分别填 "angel" / "demon"。
    /// </summary>
    public sealed class WorldMapGardenZone : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private string _owner = "angel";
        [SerializeField] private int _interactionPriority = 10;
        private Collider2D? _clickCollider;

        public bool IsWorldMapInteractionEnabled => isActiveAndEnabled;
        public int WorldMapInteractionPriority => _interactionPriority;
        public Renderer? WorldMapSortingRenderer => GetComponent<SpriteRenderer>();

        private void Awake()
        {
            _clickCollider = GetComponent<Collider2D>();
        }

        private void OnMouseDown()
        {
            if (WorldMapSceneInteractionRouter.Active is { } interactionRouter &&
                interactionRouter.IsRegistered(this))
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
            IUIRouter router = ResolveOrCreateRouter();
            router.Open(PanelId.EmotionInput, _owner);
        }

        private static IUIRouter ResolveOrCreateRouter()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? r) && r != null)
            {
                return r;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus == null)
            {
                eventBus = new EventBus();
                ServiceLocator.Register(eventBus);
            }

            IUIRouter router = new UIRouter(eventBus);
            ServiceLocator.Register<IUIRouter>(router);
            return router;
        }
    }
}
