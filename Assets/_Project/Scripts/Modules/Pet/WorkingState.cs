#nullable enable
using GeminiLab.Core.FSM;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Placeholder externally-driven working state.
    /// </summary>
    public sealed class WorkingState : IState<PetContext>
    {
        public const string StateName = "Working";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
            if (!context.RuntimeData.IsAtRequiredWorkTarget)
            {
                context.RuntimeData.WorkRequested = false;
                context.EventBus?.Publish(new PetWorkFailedEvent(context.RuntimeData.ActiveWorkTraceId, "Work target is not reached."));
                ResetWorkContext(context);
                return;
            }

            context.EventBus?.Publish(new PetWorkStartedEvent(context.RuntimeData.ActiveWorkTraceId, context.RuntimeData.TargetFurnitureId));
        }

        public void Tick(PetContext context, float deltaTime)
        {
            context.Advance(deltaTime);
            if (!context.RuntimeData.WorkRequested)
            {
                return;
            }

            if (context.RuntimeData.TimeInCurrentState >= context.Config.WorkStateTimeoutSeconds)
            {
                context.RuntimeData.WorkRequested = false;
                context.EventBus?.Publish(new PetWorkFailedEvent(context.RuntimeData.ActiveWorkTraceId, "Work timeout."));
                ResetWorkContext(context);
            }
        }

        public void FixedTick(PetContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PetContext context)
        {
        }

        private static void ResetWorkContext(PetContext context)
        {
            context.RuntimeData.ActiveWorkTraceId = string.Empty;
            context.RuntimeData.ActiveWorkMessage = string.Empty;
            context.RuntimeData.RequiredWorkTargetType = PetWorkTargetType.Any;
            context.RuntimeData.IsAtRequiredWorkTarget = false;
        }
    }
}
