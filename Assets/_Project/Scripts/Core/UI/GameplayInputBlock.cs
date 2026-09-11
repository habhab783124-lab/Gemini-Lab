#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Core.UI
{
    /// <summary>Modal UI owns a lease; releasing one panel never unlocks another panel.</summary>
    public static class GameplayInputBlock
    {
        private static readonly HashSet<object> Owners = new();
        private static int _releasedFrame = -1;
        public static bool IsBlocked => Owners.Count > 0 || (Application.isPlaying && _releasedFrame == UnityEngine.Time.frameCount);
        public static IDisposable Acquire()
        {
            var lease = new Lease();
            Owners.Add(lease);
            return lease;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Owners.Clear(); _releasedFrame = -1; }
        private sealed class Lease : IDisposable
        {
            public void Dispose()
            {
                if (Owners.Remove(this)) _releasedFrame = UnityEngine.Time.frameCount;
            }
        }
    }
}
