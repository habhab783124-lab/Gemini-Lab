#nullable enable
using GeminiLab.Core.FSM;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Centralized pet state machine transition setup.
    /// </summary>
    public static class PetStateMachineBuilder
    {
        public static StateMachine<PetContext> Build(PetContext context)
        {
            StateMachine<PetContext> machine = new(context);
            machine
                .AddState(new IdleState())
                .AddState(new MovingState())
                .AddState(new InteractingState())
                .AddState(new WorkingState())
                .AddState(new SleepingState())
                .AddAnyTransition<SleepingState>(ctx =>
                    ctx.RuntimeData.CurrentState != SleepingState.StateName &&
                    ctx.RuntimeData.Energy <= ctx.Config.SleepEnterEnergyThreshold &&
                    ctx.RuntimeData.RuntimeTimeSeconds >= ctx.RuntimeData.PreventSleepBeforeTime, priority: 100)
                .AddTransition<SleepingState, IdleState>(ctx =>
                    ctx.RuntimeData.Energy >= ctx.Config.SleepExitEnergyThreshold &&
                    ctx.RuntimeData.TimeInCurrentState >= 0.5f, priority: 50)
                .AddTransition<IdleState, MovingState>(ctx =>
                    ctx.RuntimeData.TimeInCurrentState >= 0.8f &&
                    !ctx.RuntimeData.WorkRequested &&
                    ctx.FurnitureService is not null &&
                    ctx.FurnitureService.HasPlacedFurniture &&
                    ctx.RuntimeData.Energy > ctx.Config.SleepEnterEnergyThreshold)
                .AddTransition<MovingState, InteractingState>(ctx =>
                    ctx.RuntimeData.TargetReached &&
                    !string.IsNullOrEmpty(ctx.RuntimeData.TargetFurnitureId) &&
                    ctx.RuntimeData.Energy > ctx.Config.SleepEnterEnergyThreshold)
                .AddTransition<InteractingState, IdleState>(ctx =>
                    ctx.RuntimeData.TimeInCurrentState >= 1.0f || ctx.RuntimeData.WorkRequested)
                .AddTransition<IdleState, MovingState>(ctx =>
                    ctx.RuntimeData.WorkRequested &&
                    !ctx.RuntimeData.IsAtRequiredWorkTarget &&
                    ctx.FurnitureService is not null &&
                    ctx.FurnitureService.HasPlacedFurniture,
                    priority: 20)
                .AddTransition<IdleState, WorkingState>(ctx =>
                    ctx.RuntimeData.WorkRequested &&
                    ctx.RuntimeData.IsAtRequiredWorkTarget,
                    priority: 10)
                .AddTransition<MovingState, WorkingState>(ctx =>
                    ctx.RuntimeData.WorkRequested &&
                    ctx.RuntimeData.TargetReached &&
                    ctx.RuntimeData.IsAtRequiredWorkTarget,
                    priority: 10)
                .AddTransition<MovingState, IdleState>(ctx =>
                    !ctx.RuntimeData.WorkRequested &&
                    string.IsNullOrEmpty(ctx.RuntimeData.ActiveWorkTraceId),
                    priority: 9)
                .AddTransition<InteractingState, WorkingState>(ctx =>
                    ctx.RuntimeData.WorkRequested &&
                    ctx.RuntimeData.IsAtRequiredWorkTarget,
                    priority: 10)
                .AddTransition<WorkingState, IdleState>(ctx => !ctx.RuntimeData.WorkRequested);

            machine.SetInitialState<IdleState>();
            return machine;
        }
    }
}
