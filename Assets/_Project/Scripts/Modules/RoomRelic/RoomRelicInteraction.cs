#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 纸条 / 遗留物 / 永久赠礼的点击路由。
    /// 只负责点击检测与打开对应 UI 面板，不创建或修改视觉对象。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRelicInteraction : MonoBehaviour
    {
        [SerializeField] private RoomId _roomId = RoomId.AngelRoom;
        [SerializeField] private RoomRelicKind _kind = RoomRelicKind.Note;

        private Collider2D? _collider;
        private RoomRelicView? _view;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _view = GetComponent<RoomRelicView>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_collider == null || Camera.main == null)
            {
                return;
            }

            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new(world.x, world.y);
            if (!_collider.OverlapPoint(point) ||
                !ClickOcclusionUtility.IsTopmostColliderAtWorldPoint(point, _collider))
            {
                return;
            }

            if (_view != null && !_view.HasAnyActiveTarget)
            {
                return;
            }

            Open();
        }

        public bool TryHandleWorldPoint(Vector2 worldPoint)
        {
            if (!isActiveAndEnabled) return false;
            _view ??= GetComponent<RoomRelicView>();

            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_collider == null || !_collider.enabled || !_collider.OverlapPoint(worldPoint))
            {
                return false;
            }

            if (!ClickOcclusionUtility.IsTopmostColliderAtWorldPoint(worldPoint, _collider))
            {
                return false;
            }

            if (_view != null && !_view.HasAnyActiveTarget)
            {
                return false;
            }

            Open();
            return true;
        }

        public void Open()
        {
            if (!ServiceLocator.TryResolve(out IUIRouter? router) || router is null)
            {
                return;
            }

            if (_kind == RoomRelicKind.PermanentGift)
            {
                _view ??= GetComponent<RoomRelicView>();
                if (_view == null || !ServiceLocator.TryResolve(out IRoomRelicService? gifts) || gifts == null) return;
                foreach (RoomGiftData gift in gifts.GetSnapshot(_roomId).PlacedGifts)
                    if (gift.id == _view.CurrentId) { router.Open(PanelId.RoomGiftObtained, gift); return; }
                return;
            }

            PanelId panelId = _kind switch
            {
                RoomRelicKind.Note => PanelId.RoomNote,
                RoomRelicKind.TemporaryRelic => PanelId.RoomRelicDetail,
                _ => PanelId.RoomGiftObtained
            };

            if (!router.Open(panelId, _roomId)) return;
            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
            {
                service.ConsumeCurrentItem(_roomId, _kind);
            }
        }
    }
}
