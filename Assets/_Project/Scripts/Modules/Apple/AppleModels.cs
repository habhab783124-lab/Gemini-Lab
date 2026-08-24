#nullable enable
using System;

namespace GeminiLab.Modules.Apple
{
    /// <summary>单棵祈愿树的持久化运行态。</summary>
    [Serializable]
    public struct AppleTreeState
    {
        public string TreeId;
        public long LastGeneratedUtcTicks;
        public long NextGenerationUtcTicks;
        public string GenerationDayKey;
        public int GeneratedRoundsToday;
        public int PendingCount;
        public int TotalCollected;
    }

    public readonly struct AppleChangedEvent
    {
        public AppleChangedEvent(int balance, int delta)
        {
            Balance = balance;
            Delta = delta;
        }

        public int Balance { get; }
        public int Delta { get; }
    }

    public readonly struct AppleTreeChangedEvent
    {
        public AppleTreeChangedEvent(AppleTreeState state)
        {
            State = state;
        }

        public AppleTreeState State { get; }
    }

    public readonly struct AppleTreeShakenEvent
    {
        public AppleTreeShakenEvent(string treeId, int collected)
        {
            TreeId = treeId;
            Collected = collected;
        }

        public string TreeId { get; }
        public int Collected { get; }
    }
}
