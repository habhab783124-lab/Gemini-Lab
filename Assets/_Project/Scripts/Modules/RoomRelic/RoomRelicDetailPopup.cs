#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    public sealed class RoomRelicDetailPopup : RoomRelicPanelBase
    {
        [SerializeField] private TMP_Text? _nameText;
        [SerializeField] private TMP_Text? _descriptionText;
        [SerializeField] private RoomRelicView? _iconView;

        public override PanelId Id => PanelId.RoomRelicDetail;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            RoomId roomId = payload is RoomId id ? id : RoomId.AngelRoom;
            if (!ServiceLocator.TryResolve(out IRoomRelicService? service) || service is null)
            {
                return;
            }

            RoomRelicData? relic = service.GetCurrentRelic(roomId);
            if (_nameText != null)
            {
                _nameText.text = relic?.displayName ?? string.Empty;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = relic?.observationText ?? string.Empty;
            }

            _iconView?.Apply(relic?.id);
        }
    }
}
