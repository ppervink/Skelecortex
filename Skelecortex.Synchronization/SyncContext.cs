using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Skelecortex.Synchronization
{
    internal sealed class SyncContext : ISyncContext
    {
        private readonly ICollection<SyncCoordinator> _held = new List<SyncCoordinator>();
        private volatile bool _isDisposed;
        private readonly SyncManager _syncManager;
        
        internal SyncContext (SyncManager syncManager)
        {
            _syncManager = syncManager ?? throw new ArgumentNullException(nameof(syncManager));
        }

        public bool IsDisposed => _isDisposed;

        public ISyncManager Manager { get; }

        public ISyncScope Acquire (object syncRoot)
        {
            if (syncRoot is null)
                throw new ArgumentNullException(nameof(syncRoot));

            if (_isDisposed)
                throw new ObjectDisposedException("Cannot acquire a lock from a disposed context.");

            var coordinator = _syncManager.GetCoordinator(syncRoot, true);

            if (_isDisposed)
                throw new ObjectDisposedException("Cannot acquire a lock from a disposed context.");

            coordinator.Acquire(this);

            if (!RegisterIfNotDisposed(coordinator))
                throw new ObjectDisposedException("Cannot acquire a lock from a disposed context.");

            var scope = new SyncScope(this, syncRoot);
            return scope;
        }

        public bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope)
        {
            if (syncRoot is null || _isDisposed)
            {
                syncScope = null;
                return false;
            }

            var coordinator = _syncManager.GetCoordinator(syncRoot, true);

            if (_isDisposed)
            {
                syncScope = null;
                return false;
            }

            if (!coordinator.TryAcquire(this, timeout) || !RegisterIfNotDisposed(coordinator))
            {
                syncScope = null;
                return false;
            }

            syncScope = new SyncScope(this, syncRoot);
            return true;
        }

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SyncContext () => Dispose(false);
        
        internal void Release (object syncRoot)
        {
            if (syncRoot is null)
                throw new ArgumentNullException(nameof(syncRoot));

            var coordinator = _syncManager.GetCoordinator(syncRoot, false);
            if (coordinator is null)
                return;

            coordinator.Release(this);
            lock (_held)
            {
                _held.Remove(coordinator);
            }
        }

        private bool RegisterIfNotDisposed (SyncCoordinator coordinator)
        {
            var acquired = true;
            var disposed = false;
            lock (_held)
            {
                disposed = _isDisposed;
                if (!disposed)
                {
                    _held.Add(coordinator);
                }
            }

            if (disposed)
            {
                coordinator.Release(this);
                acquired = false;
            }

            return acquired;
        }

        private void Dispose (bool disposing)
        {
            if (!disposing)
            {
                if (_held.Count > 0)
                {
                    // NOTE: In practice, this is not something that one should allow their code to do
                    // NOTE: May occur when closing/shutting the app down - will have to test/monitor to see
                    Trace.TraceWarning("An active synchronization context was allowed to be released by the garbage collector.");
                }

                return;
            }

            if (_isDisposed)
                return;

            List<SyncCoordinator> toRelease;
            lock (_held)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                toRelease = _held.ToList();
            }

            foreach (var coordinator in toRelease)
            {
                coordinator.Release(this);
            }
        }
    }
}
