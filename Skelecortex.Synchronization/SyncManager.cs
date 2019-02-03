using System;
using System.Collections.Generic;

namespace Skelecortex.Synchronization
{
    public sealed class SyncManager : ISyncManager
    {
        private readonly Dictionary<object, WeakReference<SyncCoordinator>> _coordinators =
            new Dictionary<object, WeakReference<SyncCoordinator>>();

        public ISyncScope Acquire (object syncRoot)
        {
            if (syncRoot is null)
                throw new ArgumentNullException(nameof(syncRoot));

            var context = new SyncContext(this);
            GC.SuppressFinalize(context);

            return context.Acquire(syncRoot);
        }

        public bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope)
        {
            var context = CreateContext();
            GC.SuppressFinalize(context);

            return context.TryAcquire(syncRoot, timeout, out syncScope);
        }

        public ISyncContext CreateContext () => new SyncContext(this);

        internal SyncCoordinator GetCoordinator (object syncRoot, bool create = true)
        {
            SyncCoordinator coordinator;
            lock (_coordinators)
            {
                if (!_coordinators.TryGetValue(syncRoot, out WeakReference<SyncCoordinator> coordinatorRef))
                {
                    if (!create)
                        return null;

                    coordinator = new SyncCoordinator(syncRoot);
                    coordinatorRef = new WeakReference<SyncCoordinator>(coordinator);
                    _coordinators.Add(syncRoot, coordinatorRef);
                }
                else if (!coordinatorRef.TryGetTarget(out coordinator))
                {
                    if (create)
                    {
                        coordinator = new SyncCoordinator(syncRoot);
                        coordinatorRef = new WeakReference<SyncCoordinator>(coordinator);
                        _coordinators[syncRoot] = coordinatorRef;
                    }
                    else
                    {
                        _coordinators.Remove(syncRoot);
                    }

                    RemoveDeadCoordinators();
                }
            }

            return coordinator;
        }

        private void RemoveDeadCoordinators ()
        {
            var dead = new HashSet<object>();
            foreach (var kvp in _coordinators)
            {
                if (!kvp.Value.TryGetTarget(out SyncCoordinator coordinator))
                    dead.Add(kvp.Key);
            }

            foreach (var syncRoot in dead)
            {
                _coordinators.Remove(syncRoot);
            }
        }
    }
}
