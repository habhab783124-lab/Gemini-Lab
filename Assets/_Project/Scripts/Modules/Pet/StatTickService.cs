#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Centralized stat ticking to avoid scattered direct value writes.
    /// </summary>
    public sealed class StatTickService
    {
        /// <summary>
        /// Applies one stat-tick step using current state context.
        /// </summary>
        public void Tick(PetContext context, float deltaTime)
        {
            PetRuntimeData data = context.RuntimeData;
            PetStateValueSO config = context.Config;

            if (context.IsSleeping)
            {
                data.Energy += config.SleepingEnergyRecoveryPerSecond * deltaTime;
            }
            else
            {
                data.Energy -= config.AwakeEnergyDecayPerSecond * deltaTime;
                data.Mood += config.MoodRecoveryPerSecond * deltaTime;
            }

            ClampStateValues(data);
        }

        public static void ApplyEnvironmentalBuff(PetRuntimeData data, float moodDelta, float energyDelta)
        {
            data.Mood += moodDelta;
            data.Energy += energyDelta;
            ClampStateValues(data);
        }

        private static void ClampStateValues(PetRuntimeData data)
        {
            data.Energy = Mathf.Clamp(data.Energy, 0f, 100f);
            data.Mood = Mathf.Clamp(data.Mood, 0f, 100f);
            data.Satiety = Mathf.Clamp(data.Satiety, 0f, 100f);
        }
    }
}
