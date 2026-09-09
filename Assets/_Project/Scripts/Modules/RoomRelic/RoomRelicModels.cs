#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 公寓内两个角色房间的逻辑标识。
    /// AngelRoom 对应场景右侧天使房间，DevilRoom 对应左侧恶魔房间。
    /// </summary>
    public enum RoomId
    {
        AngelRoom = 0,
        DevilRoom = 1
    }

    public enum RoomRelicKind
    {
        Note = 0,
        TemporaryRelic = 1,
        PermanentGift = 2
    }

    public enum RoomNoteVisualType
    {
        Note = 0,
        PaperBall = 1
    }

    [Serializable]
    public sealed class RoomNoteData
    {
        public string id = string.Empty;
        public string senderCharacter = string.Empty;
        public string receiverCharacter = string.Empty;
        public string content = string.Empty;
        public RoomNoteVisualType visualType = RoomNoteVisualType.Note;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class RoomRelicData
    {
        public string id = string.Empty;
        public string ownerCharacter = string.Empty;
        public RoomId targetRoom = RoomId.AngelRoom;
        public string displayName = string.Empty;
        public string observationText = string.Empty;
        public string roomVisualKey = string.Empty;
        public string roomSpritePath = string.Empty;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class RoomGiftData
    {
        public string id = string.Empty;
        public string giverCharacter = string.Empty;
        public string receiverCharacter = string.Empty;
        public string displayName = string.Empty;
        public string observationText = string.Empty;
        public string roomVisualKey = string.Empty;
        public string displaySlotId = string.Empty;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class RoomRollState
    {
        public string lastNoteRollDateIso = string.Empty;
        public string currentNoteId = string.Empty;

        public bool relicUnlocked;
        public string lastRelicRollDateIso = string.Empty;
        public string currentRelicId = string.Empty;

        public bool giftUnlocked;
        public string lastGiftRollDateIso = string.Empty;
    }

    public sealed class RoomRelicSnapshot
    {
        public RoomRelicSnapshot(
            RoomId roomId,
            RoomNoteData? currentNote,
            RoomRelicData? currentRelic,
            IReadOnlyList<RoomGiftData> placedGifts)
        {
            RoomId = roomId;
            CurrentNote = currentNote;
            CurrentRelic = currentRelic;
            PlacedGifts = placedGifts;
        }

        public RoomId RoomId { get; }
        public RoomNoteData? CurrentNote { get; }
        public RoomRelicData? CurrentRelic { get; }
        public IReadOnlyList<RoomGiftData> PlacedGifts { get; }
    }
}
