using System;

namespace Skelecortex.Synchronization
{
    /// <summary>
    /// Manages synchronization for multiple active <see cref="ISyncContext"/> instances and
    /// their associated active <see cref="ISyncScope"/> instances.
    /// </summary>
    public interface ISyncManager
    {
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
        ISyncScope Acquire (object syncRoot);

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
        ISyncContext CreateContext ();

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
        bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope);
    }
}