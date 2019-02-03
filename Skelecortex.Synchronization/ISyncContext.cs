using System;

namespace Skelecortex.Synchronization
{
    /// <summary>
    /// Provides a context in which acquired <see cref="ISyncScope"/>
    /// instances can share a lock on an object.
    /// </summary>
    /// <remarks>
    /// This is intended for when two independent tasks accomplish
    /// non-conflicting actions in the same synchronization-sensitive
    /// area at the same time within the context of a larger task.
    /// </remarks>
    public interface ISyncContext : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether or not the context has been disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets the synchronization manager used to coordinate wcope acquisition and 
        /// blocking for this instance.
        /// </summary>
        ISyncManager Manager { get; }

        /// <summary>
        /// Acquires an <see cref="ISyncScope"/> within this context for
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
        /// Attempts to acquire an <see cref="ISyncScope"/> within this context for the specified
        /// <paramref name="syncRoot"/> within the specified <paramref name="timeout"/>.
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
        /// False if the <paramref name="timeout"/> elapsed before acquisition was possible or this instance was disposed while 
        /// awaiting acquisition (and <paramref name="syncScope"/> is set to null)
        /// </returns>
        bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope);
    }
}