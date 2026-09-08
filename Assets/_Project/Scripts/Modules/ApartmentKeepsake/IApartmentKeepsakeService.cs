#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.ApartmentKeepsake
{
    public interface IApartmentKeepsakeRandom
    {
        double NextUnit();
        int NextIndex(int exclusiveMax);
    }

    /// <summary>Apartment 遗留物系统的纯业务门面；不持有任何 Scene 或 UI 引用。</summary>
    public interface IApartmentKeepsakeService
    {
        ApartmentNoteState CurrentNote { get; }
        ApartmentMementoState CurrentMemento { get; }
        IReadOnlyList<ApartmentGiftRecord> OwnedGifts { get; }
        string LastIndoorRollDateIso { get; }

        /// <summary>每日首次进入室内时调用；同一天再次调用返回 false 且不重新抽取。</summary>
        bool ProcessFirstIndoorEntry(float angelRelation, float devilRelation);

        /// <summary>亲密度在室内跨过 45 或 80 时立即执行对应首次保底。</summary>
        bool ProcessRelationThreshold(ApartmentKeepsakeOwner owner, float previousRelation, float currentRelation);

        bool IsGiftOwned(string itemId);
    }
}
