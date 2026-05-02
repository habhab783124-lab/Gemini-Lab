#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    public enum FurniturePlacementType
    {
        Floor = 0,
        Wall = 1
    }

    public enum FurnitureCategory
    {
        Unknown = 0,
        WorkDesk = 1,
        Bed = 2,
        Leisure = 3,
        Decoration = 4
    }

    public enum FurnitureInteractionType
    {
        Unknown = 0,
        WorkFocus = 1,
        SleepRest = 2,
        DecorInspect = 3,
        LeisureEngage = 4,
        SleepInBed = 5,
        InspectBookshelf = 6,
        InspectMirror = 7,
        InspectNightstand = 8,
        PlayHarp = 9,
        PlayGuitar = 10,
        PaintAtEasel = 11,
        ViewPhotoBoard = 12,
        ObservePlant = 13,
        InspectPapers = 14,
        ListenToAudio = 15,
        OrganizeStorage = 16,
        RestOnRug = 17,
        SitOnSeat = 18,
        LoungeOnSofa = 19,
        ObserveWindow = 20,
        InspectToy = 21,
        ArrangePillow = 22
    }

    [Serializable]
    public struct EnvironmentalBuff
    {
        public float MoodDelta;
        public float EnergyDelta;
    }

    [Serializable]
    public sealed class FurnitureLayoutSnapshot
    {
        public int SchemaVersion = 1;
        public FurnitureLayoutEntry[] Entries = Array.Empty<FurnitureLayoutEntry>();
    }

    [Serializable]
    public sealed class FurnitureLayoutEntry
    {
        public string FurnitureId = string.Empty;
        public string DefinitionId = string.Empty;
        public Vector2 Position;
        public float RotationZ;
    }

    public readonly struct FurnitureInteractionTarget
    {
        public FurnitureInteractionTarget(
            string furnitureId,
            string definitionId,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            float interactionDurationSeconds,
            Vector2 interactionPoint,
            float score)
        {
            FurnitureId = furnitureId;
            DefinitionId = definitionId;
            Category = category;
            InteractionType = interactionType;
            InteractionDurationSeconds = interactionDurationSeconds;
            InteractionPoint = interactionPoint;
            Score = score;
        }

        public string FurnitureId { get; }

        public string DefinitionId { get; }

        public FurnitureCategory Category { get; }

        public FurnitureInteractionType InteractionType { get; }

        public float InteractionDurationSeconds { get; }

        public Vector2 InteractionPoint { get; }

        public float Score { get; }
    }

    public readonly struct FurnitureInteractionQuery
    {
        public FurnitureInteractionQuery(FurnitureCategory requiredCategory, bool hasRequiredCategory = false)
        {
            RequiredCategory = requiredCategory;
            HasRequiredCategory = hasRequiredCategory;
        }

        public bool HasRequiredCategory { get; }
        public FurnitureCategory RequiredCategory { get; }

        public static FurnitureInteractionQuery Any => new(FurnitureCategory.Unknown, hasRequiredCategory: false);
        public static FurnitureInteractionQuery BedOnly => new(FurnitureCategory.Bed, hasRequiredCategory: true);
        public static FurnitureInteractionQuery WorkDeskOnly => new(FurnitureCategory.WorkDesk, hasRequiredCategory: true);
    }

    public static class FurnitureInteractionTypeExtensions
    {
        public static string ToDisplayLabel(this FurnitureInteractionType interactionType)
        {
            return interactionType switch
            {
                FurnitureInteractionType.WorkFocus => "工作交互",
                FurnitureInteractionType.SleepRest => "睡眠交互",
                FurnitureInteractionType.DecorInspect => "装饰观察",
                FurnitureInteractionType.LeisureEngage => "休闲交互",
                FurnitureInteractionType.SleepInBed => "上床休息",
                FurnitureInteractionType.InspectBookshelf => "看书柜",
                FurnitureInteractionType.InspectMirror => "照镜子",
                FurnitureInteractionType.InspectNightstand => "整理床头柜",
                FurnitureInteractionType.PlayHarp => "演奏竖琴",
                FurnitureInteractionType.PlayGuitar => "弹吉他",
                FurnitureInteractionType.PaintAtEasel => "看画架",
                FurnitureInteractionType.ViewPhotoBoard => "看照片板",
                FurnitureInteractionType.ObservePlant => "观察植物",
                FurnitureInteractionType.InspectPapers => "查看纸张",
                FurnitureInteractionType.ListenToAudio => "听音乐",
                FurnitureInteractionType.OrganizeStorage => "整理柜体",
                FurnitureInteractionType.RestOnRug => "地毯休息",
                FurnitureInteractionType.SitOnSeat => "坐下休息",
                FurnitureInteractionType.LoungeOnSofa => "沙发休息",
                FurnitureInteractionType.ObserveWindow => "窗台观景",
                FurnitureInteractionType.InspectToy => "抚摸玩偶",
                FurnitureInteractionType.ArrangePillow => "整理枕头",
                _ => "普通交互"
            };
        }
    }
}
