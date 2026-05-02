#nullable enable
using System.Reflection;
using GeminiLab.Modules.Furniture;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class FurnitureServiceInteractionQueryTests
    {
        [Test]
        public void TryGetBestInteractionTarget_WorkDeskOnly_ReturnsWorkDesk()
        {
            GameObject host = new("FurnitureServiceTestHost");
            FurnitureService service = host.AddComponent<FurnitureService>();
            FurnitureDefinitionSO workDesk = CreateDefinition("Furniture_WorkDesk_Test", FurnitureCategory.WorkDesk);
            FurnitureDefinitionSO bed = CreateDefinition("Furniture_Bed_Test", FurnitureCategory.Bed);

            bool placedBed = service.TryPlaceFurniture(bed, new Vector2(-1f, 0f), 0f, out Furniture? _, out string bedFailure);
            Assert.IsTrue(placedBed, bedFailure);

            bool placedWorkDesk = service.TryPlaceFurniture(workDesk, new Vector2(2f, 0f), 0f, out Furniture? _, out string workFailure);
            Assert.IsTrue(placedWorkDesk, workFailure);

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.WorkDeskOnly, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual(FurnitureCategory.WorkDesk, target.Category);
            Assert.AreEqual(FurnitureInteractionType.WorkFocus, target.InteractionType);
            Assert.Greater(target.InteractionDurationSeconds, 1f);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(workDesk);
            Object.DestroyImmediate(bed);
        }

        private static FurnitureDefinitionSO CreateDefinition(string id, FurnitureCategory category)
        {
            FurnitureDefinitionSO definition = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
            MethodInfo? configureRuntime = typeof(FurnitureDefinitionSO).GetMethod(
                "ConfigureRuntime",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(configureRuntime, "Expected FurnitureDefinitionSO.ConfigureRuntime.");
            _ = configureRuntime!.Invoke(definition, new object[]
            {
                id,
                category,
                FurniturePlacementType.Floor,
                Vector2Int.one,
                new EnvironmentalBuff(),
                null!,
                FurnitureInteractionType.Unknown,
                1f
            });

            return definition;
        }
    }
}
