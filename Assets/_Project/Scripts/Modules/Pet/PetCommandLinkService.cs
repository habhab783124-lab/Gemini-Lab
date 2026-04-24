#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.Pet
{
    public enum PetCommandType
    {
        WorkRequest = 0,
        WorkCompleted = 1,
        WorkFailed = 2
    }

    public enum PetCommandSource
    {
        PlayerMessage = 0,
        Gateway = 1,
        DebugInput = 2,
        System = 3
    }

    public enum PetWorkTargetType
    {
        Any = 0,
        WorkDesk = 1
    }

    public readonly struct PetCommandRequest
    {
        public PetCommandRequest(
            string traceId,
            PetCommandType commandType,
            PetCommandSource source,
            bool forceWake,
            int priority,
            PetWorkTargetType targetType,
            string message)
        {
            TraceId = traceId;
            CommandType = commandType;
            Source = source;
            ForceWake = forceWake;
            Priority = priority;
            TargetType = targetType;
            Message = message;
        }

        public string TraceId { get; }
        public PetCommandType CommandType { get; }
        public PetCommandSource Source { get; }
        public bool ForceWake { get; }
        public int Priority { get; }
        public PetWorkTargetType TargetType { get; }
        public string Message { get; }
    }

    public interface IPetCommandLinkService
    {
        string Enqueue(PetCommandRequest request);

        string RequestWork(bool forceWake);

        bool TryDequeue(out PetCommand command);
    }

    public readonly struct PetCommand
    {
        public PetCommand(PetCommandRequest request, long enqueueOrder)
        {
            Request = request;
            EnqueueOrder = enqueueOrder;
        }

        public PetCommandRequest Request { get; }
        public long EnqueueOrder { get; }
        public string TraceId => Request.TraceId;
        public bool ForceWake => Request.ForceWake;
        public int Priority => Request.Priority;
    }

    /// <summary>
    /// Lightweight command queue with traceable IDs.
    /// </summary>
    public sealed class PetCommandLinkService : IPetCommandLinkService
    {
        private readonly List<PetCommand> _commands = new();
        private readonly object _syncRoot = new();
        private long _enqueueOrder;

        public string Enqueue(PetCommandRequest request)
        {
            string traceId = string.IsNullOrWhiteSpace(request.TraceId) ? Guid.NewGuid().ToString("N") : request.TraceId;
            PetCommandRequest normalizedRequest = new(
                traceId,
                request.CommandType,
                request.Source,
                request.ForceWake,
                request.Priority,
                request.TargetType,
                request.Message);
            lock (_syncRoot)
            {
                PetCommand command = new(normalizedRequest, _enqueueOrder++);
                _commands.Add(command);
                _commands.Sort(CompareCommand);
            }
            return traceId;
        }

        public string RequestWork(bool forceWake)
        {
            return Enqueue(new PetCommandRequest(
                Guid.NewGuid().ToString("N"),
                PetCommandType.WorkRequest,
                PetCommandSource.DebugInput,
                forceWake,
                priority: 100,
                PetWorkTargetType.WorkDesk,
                message: string.Empty));
        }

        public bool TryDequeue(out PetCommand command)
        {
            lock (_syncRoot)
            {
                if (_commands.Count == 0)
                {
                    command = default;
                    return false;
                }

                command = _commands[0];
                _commands.RemoveAt(0);
                return true;
            }
        }

        private static int CompareCommand(PetCommand left, PetCommand right)
        {
            int priorityCompare = right.Priority.CompareTo(left.Priority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            return left.EnqueueOrder.CompareTo(right.EnqueueOrder);
        }
    }
}
