#nullable enable

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// 游戏内全部互斥场景（Single 模式加载）的逻辑标识。
    /// 代码层面一律使用此枚举，禁止直接引用场景资产路径。
    /// </summary>
    public enum SceneId
    {
        Boot = 0,
        MainMenu = 1,
        Prologue = 2,
        Apartment = 3,
        WorldMap = 4,
        DesktopOverlay = 5
    }
}
