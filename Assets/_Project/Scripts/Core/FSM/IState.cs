#nullable enable
namespace GeminiLab.Core.FSM
{
    /// <summary>
    /// Generic state contract for runtime state machines.
    /// </summary>
    public interface IState<in TContext>
    {
        /// <summary>
        /// Display name for diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Called when state becomes active.
        /// </summary>
        void Enter(TContext context);

        /// <summary>
        /// Called every frame.
        /// </summary>
        void Tick(TContext context, float deltaTime);

        /// <summary>
        /// Called on fixed update ticks.
        /// </summary>
        void FixedTick(TContext context, float fixedDeltaTime);

        /// <summary>
        /// Called when state is left.
        /// </summary>
        void Exit(TContext context);
    }
}
