#nullable enable

namespace GeminiLab.Core.FSM
{
    /// <summary>
    /// Wraps an inner state machine as a parent state.
    /// </summary>
    public sealed class SubStateMachine<TContext> : IState<TContext>
    {
        private readonly StateMachine<TContext> _innerMachine;
        private readonly string _name;

        public SubStateMachine(string name, StateMachine<TContext> innerMachine)
        {
            _name = name;
            _innerMachine = innerMachine;
        }

        public string Name => _name;

        public void Enter(TContext context)
        {
            // Inner machine state should be set by builder before activation.
        }

        public void Tick(TContext context, float deltaTime)
        {
            _innerMachine.Tick(deltaTime);
        }

        public void FixedTick(TContext context, float fixedDeltaTime)
        {
            _innerMachine.FixedTick(fixedDeltaTime);
        }

        public void Exit(TContext context)
        {
            // No-op on exit by default.
        }
    }
}
