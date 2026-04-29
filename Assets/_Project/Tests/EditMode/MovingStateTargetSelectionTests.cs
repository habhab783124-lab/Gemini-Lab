#nullable enable
using System;
using System.Reflection;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class MovingStateTargetSelectionTests
    {
        [Test]
        public void Enter_DaytimeModerateEnergy_PrefersLeisureOverBed()
        {
            using TestFixtureScope scope = CreateFixture();
            PetContext context = CreateContext(scope.Service, energy: 35f, mood: 55f, hour: 14);

            MovingState state = new();
            state.Enter(context);

            Assert.AreEqual(FurnitureCategory.Leisure, context.RuntimeData.TargetFurnitureCategory);
        }

        [Test]
        public void Enter_NighttimeModerateEnergy_PrefersBed()
        {
            using TestFixtureScope scope = CreateFixture();
            PetContext context = CreateContext(scope.Service, energy: 35f, mood: 55f, hour: 23);

            MovingState state = new();
            state.Enter(context);

            Assert.AreEqual(FurnitureCategory.Bed, context.RuntimeData.TargetFurnitureCategory);
        }

        [Test]
        public void Enter_CriticalEnergy_DaytimeStillPrefersBed()
        {
            using TestFixtureScope scope = CreateFixture();
            PetContext context = CreateContext(scope.Service, energy: 10f, mood: 60f, hour: 14);

            MovingState state = new();
            state.Enter(context);

            Assert.AreEqual(FurnitureCategory.Bed, context.RuntimeData.TargetFurnitureCategory);
        }

        private static PetContext CreateContext(IFurnitureService furnitureService, float energy, float mood, int hour)
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.SleepEnterEnergyThreshold = 20f;
            config.DaytimeBedSeekEnergyThreshold = 15f;
            config.NighttimeBedSeekEnergyThreshold = 40f;
            config.LeisureSeekMoodThreshold = 85f;
            config.NightHourStart = 22;
            config.NightHourEnd = 6;

            PetRuntimeData runtimeData = new()
            {
                Energy = energy,
                Mood = mood,
                Satiety = 60f,
                Position = Vector2.zero
            };

            PetContext context = new(runtimeData, config, furnitureService: furnitureService)
            {
                NowProvider = () => new DateTime(2026, 4, 28, hour, 0, 0)
            };
            context.EnterState(IdleState.StateName);
            return context;
        }

        private static TestFixtureScope CreateFixture()
        {
            GameObject host = new("MovingStateTargetSelectionTests_FurnitureService");
            FurnitureService service = host.AddComponent<FurnitureService>();

            CreateFurniture(service, "Furniture_Bed_Angel_001", FurnitureCategory.Bed, new Vector2(-2f, 0f));
            CreateFurniture(service, "Furniture_Harp_Angel_001", FurnitureCategory.Leisure, new Vector2(2f, 0f));

            return new TestFixtureScope(host);
        }

        private static void CreateFurniture(FurnitureService service, string id, FurnitureCategory category, Vector2 position)
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
                category == FurnitureCategory.Bed ? new Vector2Int(2, 1) : Vector2Int.one,
                category == FurnitureCategory.Bed
                    ? new EnvironmentalBuff { MoodDelta = 2f, EnergyDelta = 6f }
                    : new EnvironmentalBuff { MoodDelta = 5f, EnergyDelta = 0f },
                null!
            });

            bool placed = service.TryPlaceFurniture(definition, position, 0f, out Furniture? _, out string reason);
            Assert.IsTrue(placed, reason);
        }

        private sealed class TestFixtureScope : IDisposable
        {
            private readonly GameObject _host;

            public TestFixtureScope(GameObject host)
            {
                _host = host;
                Service = host.GetComponent<FurnitureService>()!;
            }

            public FurnitureService Service { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_host);
            }
        }
    }
}
