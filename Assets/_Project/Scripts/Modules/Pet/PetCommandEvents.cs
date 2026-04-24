#nullable enable

namespace GeminiLab.Modules.Pet
{
    public readonly struct PetCommandAcceptedEvent
    {
        public PetCommandAcceptedEvent(string traceId, bool forceWake, PetCommandType commandType, PetCommandSource source)
        {
            TraceId = traceId;
            ForceWake = forceWake;
            CommandType = commandType;
            Source = source;
        }

        public string TraceId { get; }
        public bool ForceWake { get; }
        public PetCommandType CommandType { get; }
        public PetCommandSource Source { get; }
    }

    public readonly struct PetWakePenaltyAppliedEvent
    {
        public PetWakePenaltyAppliedEvent(string traceId, float moodDelta)
        {
            TraceId = traceId;
            MoodDelta = moodDelta;
        }

        public string TraceId { get; }
        public float MoodDelta { get; }
    }

    public readonly struct PetCommandRejectedEvent
    {
        public PetCommandRejectedEvent(string traceId, string reason)
        {
            TraceId = traceId;
            Reason = reason;
        }

        public string TraceId { get; }
        public string Reason { get; }
    }

    public readonly struct PetWorkStartedEvent
    {
        public PetWorkStartedEvent(string traceId, string furnitureId)
        {
            TraceId = traceId;
            FurnitureId = furnitureId;
        }

        public string TraceId { get; }
        public string FurnitureId { get; }
    }

    public readonly struct PetWorkCompletedEvent
    {
        public PetWorkCompletedEvent(string traceId, string message)
        {
            TraceId = traceId;
            Message = message;
        }

        public string TraceId { get; }
        public string Message { get; }
    }

    public readonly struct PetWorkFailedEvent
    {
        public PetWorkFailedEvent(string traceId, string reason)
        {
            TraceId = traceId;
            Reason = reason;
        }

        public string TraceId { get; }
        public string Reason { get; }
    }
}
