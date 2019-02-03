using System;
using System.Collections.Generic;

namespace Skelecortex.Synchronization
{
    /// <summary>
    /// Manages synchronization for multiple active <see cref="ISyncContext"/> instances and
    /// their associated active <see cref="ISyncScope"/> instances.
    /// </summary>
    public sealed class SyncManager : ISyncManager
    {
        private readonly Dictionary<object, WeakReference<SyncCoordinator>> _coordinators =
            new Dictionary<object, WeakReference<SyncCoordinator>>();

        /// <summary>
        /// Acquires an <see cref="ISyncScope"/> within its own <see cref="ISyncContext"/> for
        /// the specified <paramref name="syncRoot"/>. See <see cref="ISyncScope"/> for
        /// more information on the blocking behavior of an acquired <see cref="ISyncScope"/>.
        /// </summary>
        /// <param name="syncRoot">
        /// An object used to identify the resource for which the acquired <see cref="ISyncScope"/> 
        /// will provide synchronization.
        /// </param>
        /// <returns>The <see cref="ISyncScope"/> that was acquired.</returns>
        public ISyncScope Acquire (object syncRoot)
        {
            if (syncRoot is null)
                throw new ArgumentNullException(nameof(syncRoot));

            var context = new SyncContext(this);
            GC.SuppressFinalize(context);

            return context.Acquire(syncRoot);
        }

        /// <summary>
        /// Attempts to acquire an <see cref="ISyncScope"/> within its own <see cref="ISyncContext"/> for 
        /// the specified <paramref name="syncRoot"/> within the specified <paramref name="timeout"/>.
        /// See <see cref="ISyncScope"/> for more information on the blocking behavior of an acquired <see cref="ISyncScope"/>.
        /// </summary>
        /// <param name="syncRoot">
        /// An object used to identify the resource for which the acquired <see cref="ISyncScope"/> 
        /// will provide synchronization.
        /// </param>
        /// <param name="timeout">The time, in milliseconds, to wait for the <see cref="ISyncScope"/> to be acquired (<see cref="System.Threading.Timeout.Infinite"/> to wait indefinitely).</param>
        /// <param name="syncScope">If successful, returns the <see cref="ISyncScope"/> that was acquired.</param>
        /// <returns>
        /// True if the <see cref="ISyncScope"/> was acquired within the specified timeout (and returned in <paramref name="syncScope"/>);
        /// False if the <paramref name="timeout"/> elapsed before acquisition was possible (and <paramref name="syncScope"/> is set to null).
        /// </returns>
        public bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope)
        {
            var context = CreateContext();
            GC.SuppressFinalize(context);

            return context.TryAcquire(syncRoot, timeout, out syncScope);
        }

        /// <summary>
        /// Creates a <see cref="ISyncContext"/> in which multiple tasks
        /// share the right to acquire an <see cref="ISyncScope"/> for the same
        /// object at the same time.
        /// </summary>
        /// <returns>
        /// An <see cref="ISyncContext"/> in which multiple tasks
        /// share the right to acquire an <see cref="ISyncScope"/> for the same
        /// object at the same time.
        /// </returns>
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
