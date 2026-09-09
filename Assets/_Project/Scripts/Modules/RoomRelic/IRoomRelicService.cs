#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 室内遗留物系统门面。
    /// 所有业务代码通过此接口读取状态，不直接访问场景节点或持久化内部结构。
    /// </summary>
    public interface IRoomRelicService
    {
        RoomRelicSnapshot GetSnapshot(RoomId roomId);

        /// <summary>当天第一次进入房间时执行完整判定；同一天内幂等。</summary>
        void ProcessRoomEntry(RoomId roomId);

        /// <summary>更新玩家当前所在房间，用于亲密度跨过临界值时立即触发首次遗留物/赠礼。</summary>
        void SetCurrentRoom(RoomId roomId);

        /// <summary>玩家离开房间时清除当前房间。</summary>
        void ClearCurrentRoom(RoomId roomId);

        void ConsumeCurrentItem(RoomId roomId, RoomRelicKind kind);

        RoomNoteData? GetCurrentNote(RoomId roomId);
        RoomRelicData? GetCurrentRelic(RoomId roomId);
        IReadOnlyList<RoomGiftData> GetPlacedGifts(RoomId roomId);

        event Action<RoomRelicStateChangedEvent> StateChanged;
        event Action<RoomGiftObtainedEvent> GiftObtained;
    }
}
