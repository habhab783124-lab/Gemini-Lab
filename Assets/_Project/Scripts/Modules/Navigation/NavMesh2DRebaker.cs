#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Coalesces rapid rebuild requests and exposes revision changes.
    /// </summary>
    public sealed class NavMesh2DRebaker
    {
        private int _revision;
        private bool _rebuildScheduled;

        public int Revision => _revision;

        public async Task<int> RebuildAsync(CancellationToken cancellationToken = default)
        {
            if (_rebuildScheduled)
            {
                await Task.Yield();
                return _revision;
            }

            _rebuildScheduled = true;
            try
            {
                await Task.Delay(1, cancellationToken);
                _revision++;
                return _revision;
            }
            finally
            {
                _rebuildScheduled = false;
            }
        }
    }
}
