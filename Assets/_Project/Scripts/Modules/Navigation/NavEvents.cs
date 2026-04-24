#nullable enable

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Event emitted after navigation data has been rebuilt.
    /// </summary>
    public readonly struct NavMeshRebuiltEvent
    {
        public NavMeshRebuiltEvent(int revision, string reason)
        {
            Revision = revision;
            Reason = reason;
        }

        public int Revision { get; }

        public string Reason { get; }
    }

    /// <summary>
    /// Event emitted when path request fails.
    /// </summary>
    public readonly struct PathRequestFailedEvent
    {
        public PathRequestFailedEvent(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
