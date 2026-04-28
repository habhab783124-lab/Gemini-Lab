#nullable enable
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Applies furniture interaction effects.
    /// </summary>
    public sealed class InteractingState : IState<PetContext>
    {
        public const string StateName = "Interacting";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
            if (context.FurnitureService is not null &&
                context.FurnitureService.TryConsumeInteractionBuff(context.RuntimeData.TargetFurnitureId, out EnvironmentalBuff buff))
            {
                StatTickService.ApplyEnvironmentalBuff(context.RuntimeData, buff.MoodDelta, buff.EnergyDelta);
                context.RuntimeData.LastInteractionFurnitureId = context.RuntimeData.TargetFurnitureId;
                context.RuntimeData.LastInteractionSummary =
                    $"{context.RuntimeData.TargetFurnitureCategory} (Mood {FormatSigned(buff.MoodDelta)}, Energy {FormatSigned(buff.EnergyDelta)})";
            }
        }

        public void Tick(PetContext context, float deltaTime)
        {
            context.Advance(deltaTime);
        }

        public void FixedTick(PetContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PetContext context)
        {
        }

        private static string FormatSigned(float value)
        {
            return value >= 0f ? $"+{value:0.#}" : value.ToString("0.#");
        }
    }
}
