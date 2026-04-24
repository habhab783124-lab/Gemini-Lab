#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Navigation facade used by gameplay modules.
    /// </summary>
    public interface INavigationService
    {
        int Revision { get; }

        bool TryRequestPath(Vector2 from, Vector2 to, out NavigationPath path);

        Vector2 SampleValidPosition(Vector2 candidate, float radius = 0.5f);

        Task RebuildAsync(string reason, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Read-only path result object.
    /// </summary>
    public readonly struct NavigationPath
    {
        public NavigationPath(IReadOnlyList<Vector2> points)
        {
            Points = points;
        }

        public IReadOnlyList<Vector2> Points { get; }
    }
}
