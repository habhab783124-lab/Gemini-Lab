#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// UI 面板的逻辑标识。面板是覆盖在当前场景之上的 Canvas 子对象，不是场景。
    /// </summary>
    public enum PanelId
    {
        None = 0,

        // 主菜单相关
        SaveSlots = 10,
        Settings = 11,

        // 侧边栏五件套
        PetStatus = 20,
        Tarot = 21,
        Collection = 22,
        Inventory = 23,
        Garden = 24,
        SpaceSys = 25,

        // 情绪花园
        EmotionInput = 26,
        WeeklyGardenView = 27,
        EmotionCollection = 28,
        DailySummaryMailbox = 29,

        // 室内遗留物
        RoomNote = 29,
        RoomRelicDetail = 30,
        RoomGiftObtained = 31,

        // 通用
        ConfirmDialog = 90
    }
}
