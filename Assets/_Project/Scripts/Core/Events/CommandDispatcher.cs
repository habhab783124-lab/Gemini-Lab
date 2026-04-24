#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Core.Events
{
    /// <summary>
    /// Dispatches commands immediately or in queued batches.
    /// </summary>
    public sealed class CommandDispatcher
    {
        private readonly Queue<ICommand> _queue = new();

        /// <summary>
        /// Raised after each command execution.
        /// </summary>
        public event Action<ICommand>? CommandExecuted;

        /// <summary>
        /// Executes command immediately.
        /// </summary>
        public void Dispatch(ICommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            CommandExecuted?.Invoke(command);
        }

        /// <summary>
        /// Enqueues a command for deferred execution.
        /// </summary>
        public void Enqueue(ICommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _queue.Enqueue(command);
        }

        /// <summary>
        /// Executes all queued commands in FIFO order.
        /// </summary>
        public void Flush()
        {
            while (_queue.Count > 0)
            {
                Dispatch(_queue.Dequeue());
            }
        }
    }
}
