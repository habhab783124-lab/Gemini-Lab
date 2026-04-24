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
        public FurnitureInteractionTarget(string furnitureId, string definitionId, FurnitureCategory category, Vector2 interactionPoint, float score)
        {
            FurnitureId = furnitureId;
            DefinitionId = definitionId;
            Category = category;
            InteractionPoint = interactionPoint;
            Score = score;
        }

        public string FurnitureId { get; }

        public string DefinitionId { get; }

        public FurnitureCategory Category { get; }

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
        public static FurnitureInteractionQuery WorkDeskOnly => new(FurnitureCategory.WorkDesk, hasRequiredCategory: true);
    }
}
