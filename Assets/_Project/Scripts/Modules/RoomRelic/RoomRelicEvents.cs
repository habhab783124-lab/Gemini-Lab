#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.RoomRelic
{
    public readonly struct RoomRelicStateChangedEvent
    {
        public RoomRelicStateChangedEvent(RoomId roomId, RoomRelicSnapshot snapshot)
        {
            RoomId = roomId;
            Snapshot = snapshot;
        }

        public RoomId RoomId { get; }
        public RoomRelicSnapshot Snapshot { get; }
    }

    public readonly struct RoomGiftObtainedEvent
    {
        public RoomGiftObtainedEvent(RoomId roomId, RoomGiftData gift)
        {
            RoomId = roomId;
            Gift = gift;
        }

        public RoomId RoomId { get; }
        public RoomGiftData Gift { get; }
    }
}
