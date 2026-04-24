#nullable enable
using System;

namespace GeminiLab.Core.FSM
{
    /// <summary>
    /// Represents a conditional transition between two states.
    /// </summary>
    public sealed class StateTransition<TContext>
    {
        /// <summary>
        /// Source state type; null means AnyState transition.
        /// </summary>
        public Type? FromStateType { get; }

        /// <summary>
        /// Destination state type.
        /// </summary>
        public Type ToStateType { get; }

        /// <summary>
        /// Transition condition predicate.
        /// </summary>
        public Func<TContext, bool> Condition { get; }

        /// <summary>
        /// Higher priority transitions are evaluated first.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Creates a new transition.
        /// </summary>
        public StateTransition(Type? fromStateType, Type toStateType, Func<TContext, bool> condition, int priority = 0)
        {
            FromStateType = fromStateType;
            ToStateType = toStateType ?? throw new ArgumentNullException(nameof(toStateType));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Priority = priority;
        }
    }
}
