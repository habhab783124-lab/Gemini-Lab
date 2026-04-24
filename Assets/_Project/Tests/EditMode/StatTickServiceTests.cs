#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class StatTickServiceTests
    {
        [Test]
        public void Tick_Awake_DecreasesEnergyAndRecoversMood()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.AwakeEnergyDecayPerSecond = 10f;
            config.MoodRecoveryPerSecond = 5f;
            config.SleepEnterEnergyThreshold = 20f;

            PetRuntimeData data = new()
            {
                Energy = 100f,
                Mood = 20f
            };

            PetContext context = new(data, config);
            context.EnterState(IdleState.StateName);

            StatTickService service = new();
            service.Tick(context, 1f);

            Assert.AreEqual(90f, data.Energy, 0.01f);
            Assert.AreEqual(25f, data.Mood, 0.01f);
        }

        [Test]
        public void Tick_Sleeping_IncreasesEnergy()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.SleepingEnergyRecoveryPerSecond = 12f;

            PetRuntimeData data = new()
            {
                Energy = 40f,
                Mood = 50f
            };

            PetContext context = new(data, config);
            context.EnterState(SleepingState.StateName);

            StatTickService service = new();
            service.Tick(context, 1f);

            Assert.AreEqual(52f, data.Energy, 0.01f);
        }
    }
}
