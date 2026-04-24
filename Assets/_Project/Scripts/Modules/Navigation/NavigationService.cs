#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Default 2D navigation service implementation.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private readonly NavMesh2DRebaker _rebaker = new();

        public int Revision => _rebaker.Revision;

        public bool TryRequestPath(Vector2 from, Vector2 to, out NavigationPath path)
        {
            if (Vector2.Distance(from, to) <= 0.01f)
            {
                path = new NavigationPath(new[] { from });
                return true;
            }

            path = new NavigationPath(new[] { from, to });
            return true;
        }

        public Vector2 SampleValidPosition(Vector2 candidate, float radius = 0.5f)
        {
            _ = radius;
            return candidate;
        }

        public async Task RebuildAsync(string reason, CancellationToken cancellationToken = default)
        {
            int revision = await _rebaker.RebuildAsync(cancellationToken);
            if (ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus.Publish(new NavMeshRebuiltEvent(revision, reason));
            }
        }
    }
}
