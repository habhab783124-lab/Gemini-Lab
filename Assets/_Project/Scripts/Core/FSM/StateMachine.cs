#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeminiLab.Core.FSM
{
    /// <summary>
    /// Generic finite state machine with conditional transitions.
    /// </summary>
    public class StateMachine<TContext>
    {
        private readonly Dictionary<Type, IState<TContext>> _states = new();
        private readonly List<StateTransition<TContext>> _transitions = new();

        /// <summary>
        /// Raised when state changes.
        /// </summary>
        public event Action<string, string>? StateChanged;

        /// <summary>
        /// Machine context passed into states and predicates.
        /// </summary>
        public TContext Context { get; }

        /// <summary>
        /// Current active state.
        /// </summary>
        public IState<TContext>? CurrentState { get; private set; }

        public StateMachine(TContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Adds or replaces a state instance.
        /// </summary>
        public StateMachine<TContext> AddState(IState<TContext> state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _states[state.GetType()] = state;
            return this;
        }

        /// <summary>
        /// Adds a transition between two registered state types.
        /// </summary>
        public StateMachine<TContext> AddTransition<TFrom, TTo>(Func<TContext, bool> condition, int priority = 0)
            where TFrom : IState<TContext>
            where TTo : IState<TContext>
        {
            _transitions.Add(new StateTransition<TContext>(typeof(TFrom), typeof(TTo), condition, priority));
            return this;
        }

        /// <summary>
        /// Adds an AnyState transition into a destination state.
        /// </summary>
        public StateMachine<TContext> AddAnyTransition<TTo>(Func<TContext, bool> condition, int priority = 0)
            where TTo : IState<TContext>
        {
            _transitions.Add(new StateTransition<TContext>(null, typeof(TTo), condition, priority));
            return this;
        }

        /// <summary>
        /// Sets initial state and enters it immediately.
        /// </summary>
        public void SetInitialState<TState>() where TState : IState<TContext>
        {
            if (!_states.TryGetValue(typeof(TState), out IState<TContext>? state))
            {
                throw new InvalidOperationException($"State not registered: {typeof(TState).Name}");
            }

            CurrentState = state;
            CurrentState.Enter(Context);
        }

        /// <summary>
        /// Executes transition checks then ticks current state.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TryTransition();
            CurrentState?.Tick(Context, deltaTime);
        }

        /// <summary>
        /// Ticks fixed-step logic on current state.
        /// </summary>
        public void FixedTick(float fixedDeltaTime)
        {
            CurrentState?.FixedTick(Context, fixedDeltaTime);
        }

        /// <summary>
        /// Forces transition to a specific state type.
        /// </summary>
        public void ForceChangeState<TState>() where TState : IState<TContext>
        {
            ChangeState(typeof(TState));
        }

        private void TryTransition()
        {
            if (CurrentState is null)
            {
                return;
            }

            Type currentType = CurrentState.GetType();
            StateTransition<TContext>? selected = _transitions
                .Where(t => t.FromStateType is null || t.FromStateType == currentType)
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault(t => t.Condition(Context));

            if (selected is null || selected.ToStateType == currentType)
            {
                return;
            }

            ChangeState(selected.ToStateType);
        }

        private void ChangeState(Type targetType)
        {
            if (!_states.TryGetValue(targetType, out IState<TContext>? nextState))
            {
                throw new InvalidOperationException($"Target state not registered: {targetType.Name}");
            }

            string from = CurrentState?.Name ?? "<None>";
            CurrentState?.Exit(Context);
            CurrentState = nextState;
            CurrentState.Enter(Context);
            StateChanged?.Invoke(from, CurrentState.Name);
        }
    }
}
