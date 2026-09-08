#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.ApartmentKeepsake
{
    public enum ApartmentKeepsakeOwner
    {
        Angel = 0,
        Devil = 1
    }

    /// <summary>当天室内可能出现的一张双方留言。</summary>
    [Serializable]
    public struct ApartmentNoteState
    {
        public bool IsPresent;
        public string NoteId;
        public ApartmentKeepsakeOwner Sender;
        public ApartmentKeepsakeOwner Recipient;
        public string Content;
        public int SpawnIndex;
    }

    /// <summary>亲密度 45–79 时，室内当前唯一的临时遗留物。</summary>
    [Serializable]
    public struct ApartmentMementoState
    {
        public bool IsPresent;
        public string ItemId;
        public ApartmentKeepsakeOwner Owner;
    }

    /// <summary>亲密度达到 80 后永久保存的一件赠礼。</summary>
    [Serializable]
    public struct ApartmentGiftRecord
    {
        public string ItemId;
        public ApartmentKeepsakeOwner Owner;
        public string AcquiredDateIso;
    }

    public enum ApartmentKeepsakeItemKind
    {
        Memento = 0,
        Gift = 1
    }

    /// <summary>稳定逻辑 ID 与首轮本地展示文本；Sprite 映射仍保存在 Apartment Scene。</summary>
    public readonly struct ApartmentKeepsakeItemDefinition
    {
        public ApartmentKeepsakeItemDefinition(
            string id,
            ApartmentKeepsakeOwner owner,
            ApartmentKeepsakeItemKind kind,
            string title,
            string description)
        {
            Id = id;
            Owner = owner;
            Kind = kind;
            Title = title;
            Description = description;
        }

        public string Id { get; }
        public ApartmentKeepsakeOwner Owner { get; }
        public ApartmentKeepsakeItemKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
    }

    /// <summary>
    /// 首轮可替换目录。业务只保存这里的稳定 ID；美术替换不需要改存档格式。
    /// </summary>
    public static class ApartmentKeepsakeCatalog
    {
        public const string AngelPrayerStatue = "memento.angel.prayer_statue";
        public const string AngelFeatherPen = "memento.angel.feather_pen";
        public const string AngelRoundMirror = "memento.angel.round_mirror";
        public const string DevilPinkDoll = "memento.devil.pink_doll";
        public const string DevilBatFigure = "memento.devil.bat_figure";
        public const string DevilSkullPlush = "memento.devil.skull_plush";

        public const string AngelBadgeGift = "gift.angel.badge";
        public const string AngelAcrylicGift = "gift.angel.acrylic_sign";
        public const string AngelPhotoGift = "gift.angel.photo";
        public const string DevilBadgeGift = "gift.devil.badge";
        public const string DevilPolaroidGift = "gift.devil.polaroid";
        public const string DevilPostcardGift = "gift.devil.postcard";

        private static readonly ApartmentKeepsakeItemDefinition[] s_items =
        {
            new(AngelPrayerStatue, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Memento, "祈祷小像", "天使曾认真擦拭过的小像，底座还留着温暖的触感。"),
            new(AngelFeatherPen, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Memento, "羽毛笔筒", "一支被妥善收好的羽毛笔，纸页边缘留有浅浅墨迹。"),
            new(AngelRoundMirror, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Memento, "晨光圆镜", "镜面总像比房间更早一步接住清晨。"),
            new(DevilPinkDoll, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Memento, "粉角玩偶", "嘴上说着只是随手一放，却被摆得端端正正。"),
            new(DevilBatFigure, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Memento, "蝙蝠摆件", "小翅膀有些歪，像是被反复拿起来把玩过。"),
            new(DevilSkullPlush, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Memento, "骷髅软偶", "看起来凶巴巴，摸上去却意外柔软。"),
            new(AngelBadgeGift, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Gift, "天使徽章", "作为重要陪伴证明而送出的天使徽章。"),
            new(AngelAcrylicGift, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Gift, "亚克力立牌", "把两个人一起留在桌边的小小立牌。"),
            new(AngelPhotoGift, ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Gift, "双人合照", "被天使珍重挑选并写下日期的合照。"),
            new(DevilBadgeGift, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Gift, "恶魔徽章", "恶魔声称只是多出来的一枚限定徽章。"),
            new(DevilPolaroidGift, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Gift, "拍立得", "画面有点晃，却把那天的笑容完整留了下来。"),
            new(DevilPostcardGift, ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Gift, "双人明信片", "背面写着一句不肯当面说出口的话。")
        };

        private static readonly ApartmentKeepsakeItemDefinition[] s_angelMementos = Select(ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Memento);
        private static readonly ApartmentKeepsakeItemDefinition[] s_devilMementos = Select(ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Memento);
        private static readonly ApartmentKeepsakeItemDefinition[] s_angelGifts = Select(ApartmentKeepsakeOwner.Angel, ApartmentKeepsakeItemKind.Gift);
        private static readonly ApartmentKeepsakeItemDefinition[] s_devilGifts = Select(ApartmentKeepsakeOwner.Devil, ApartmentKeepsakeItemKind.Gift);

        public static IReadOnlyList<ApartmentKeepsakeItemDefinition> All => s_items;

        public static IReadOnlyList<ApartmentKeepsakeItemDefinition> GetPool(
            ApartmentKeepsakeOwner owner,
            ApartmentKeepsakeItemKind kind)
        {
            if (kind == ApartmentKeepsakeItemKind.Memento)
            {
                return owner == ApartmentKeepsakeOwner.Devil ? s_devilMementos : s_angelMementos;
            }

            return owner == ApartmentKeepsakeOwner.Devil ? s_devilGifts : s_angelGifts;
        }

        public static bool TryGet(string itemId, out ApartmentKeepsakeItemDefinition definition)
        {
            for (int i = 0; i < s_items.Length; i++)
            {
                if (string.Equals(s_items[i].Id, itemId, StringComparison.Ordinal))
                {
                    definition = s_items[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        private static ApartmentKeepsakeItemDefinition[] Select(ApartmentKeepsakeOwner owner, ApartmentKeepsakeItemKind kind)
        {
            var result = new List<ApartmentKeepsakeItemDefinition>();
            for (int i = 0; i < s_items.Length; i++)
            {
                if (s_items[i].Owner == owner && s_items[i].Kind == kind)
                {
                    result.Add(s_items[i]);
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>状态改变时通知 Scene 刷新；只有业务变更才要求 autosave，读档恢复不回写。</summary>
    public readonly struct ApartmentKeepsakeStateChangedEvent
    {
        public ApartmentKeepsakeStateChangedEvent(bool requestAutosave)
        {
            RequestAutosave = requestAutosave;
        }

        public bool RequestAutosave { get; }
    }
}
