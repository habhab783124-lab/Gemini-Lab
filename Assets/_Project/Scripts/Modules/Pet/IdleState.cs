#nullable enable
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Default roaming wait state.
    /// </summary>
    public sealed class IdleState : IState<PetContext>
    {
        public const string StateName = "Idle";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
            context.RuntimeData.TargetReached = false;
            context.RuntimeData.TargetFurnitureId = string.Empty;
            context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
            context.RuntimeData.ActivePath.Clear();
            context.RuntimeData.PathIndex = 0;
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
    }
}
