#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// 默认 SceneId → scene name 映射。
    /// scene name 必须与 EditorBuildSettings 中登记的 scene 文件名一致（不含 .unity 后缀）。
    /// </summary>
    public sealed class DefaultSceneCatalog : ISceneCatalog
    {
        private static readonly IReadOnlyDictionary<SceneId, string> Map = new Dictionary<SceneId, string>
        {
            { SceneId.Boot, "Boot" },
            { SceneId.MainMenu, "MainMenu" },
            { SceneId.Prologue, "Prologue" },
            { SceneId.Apartment, "Apartment_Main" },
            { SceneId.WorldMap, "WorldMap_Main" },
            { SceneId.DesktopOverlay, "Desktop_Overlay" }
        };

        public string GetSceneName(SceneId id)
        {
            if (Map.TryGetValue(id, out string? name))
            {
                return name;
            }

            throw new ArgumentOutOfRangeException(nameof(id), id, "未登记的 SceneId");
        }
    }
}
