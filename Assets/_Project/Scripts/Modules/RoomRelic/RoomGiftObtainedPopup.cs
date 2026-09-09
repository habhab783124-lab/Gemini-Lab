#nullable enable
using GeminiLab.Core.UI;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    public sealed class RoomGiftObtainedPopup : RoomRelicPanelBase
    {
        [SerializeField] private TMP_Text? _giftNameText;
        [SerializeField] private TMP_Text? _hintText;
        [SerializeField] private RoomRelicView? _iconView;

        public override PanelId Id => PanelId.RoomGiftObtained;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            RoomGiftData? gift = payload as RoomGiftData;
            if (_giftNameText != null)
            {
                _giftNameText.text = gift?.displayName ?? string.Empty;
            }

            if (_hintText != null)
            {
                _hintText.text = "赠礼已被保存到房间中。";
            }

            _iconView?.Apply(gift?.id);
        }
    }
}
