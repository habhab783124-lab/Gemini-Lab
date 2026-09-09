#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    public sealed class RoomNotePopup : RoomRelicPanelBase
    {
        [SerializeField] private TMP_Text? _contentText;

        public override PanelId Id => PanelId.RoomNote;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            RoomId roomId = payload is RoomId id ? id : RoomId.AngelRoom;
            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null &&
                _contentText != null)
            {
                _contentText.text = service.GetCurrentNote(roomId)?.content ?? string.Empty;
            }
        }
    }
}
