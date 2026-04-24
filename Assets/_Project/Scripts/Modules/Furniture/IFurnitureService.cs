#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    public interface IFurnitureService
    {
        IReadOnlyList<FurnitureDefinitionSO> GetBuildPalette();

        IReadOnlyList<Furniture> GetPlacedFurniture();

        bool HasPlacedFurniture { get; }

        bool TryPlaceFurniture(FurnitureDefinitionSO definition, Vector2 position, float rotationZ, out Furniture? furniture, out string failureReason);

        bool TryRemoveNearestFurniture(Vector2 position, float maxDistance, out string removedFurnitureId);

        bool TryGetBestInteractionTarget(Vector2 origin, out FurnitureInteractionTarget target);

        bool TryGetBestInteractionTarget(Vector2 origin, FurnitureInteractionQuery query, out FurnitureInteractionTarget target);

        bool TryConsumeInteractionBuff(string furnitureId, out EnvironmentalBuff buff);

        FurnitureLayoutSnapshot CaptureLayout();

        void RestoreLayout(FurnitureLayoutSnapshot snapshot);
    }
}
