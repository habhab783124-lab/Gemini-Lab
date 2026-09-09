#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    [CreateAssetMenu(menuName = "GeminiLab/RoomRelic/RoomRelicCatalog", fileName = "RoomRelicCatalog")]
    public sealed class RoomRelicCatalogSO : ScriptableObject
    {
        public RoomNoteData[] notes = Array.Empty<RoomNoteData>();
        public RoomRelicData[] relics = Array.Empty<RoomRelicData>();
        public RoomGiftData[] gifts = Array.Empty<RoomGiftData>();

        public IReadOnlyList<RoomNoteData> Notes => notes;
        public IReadOnlyList<RoomRelicData> Relics => relics;
        public IReadOnlyList<RoomGiftData> Gifts => gifts;
    }
}
