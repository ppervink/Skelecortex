using System;

namespace Skelecortex.Synchronization
{
    /// <summary>
    /// Represents a blocking synchronization scope for a given <see cref="SyncRoot"/>. 
    /// Any attempt to acquire an <see cref="ISyncScope"/> outside of the given 
    /// <see cref="Context"/> will be blocked by the <see cref="ISyncContext.Manager"/>
    /// until this instance is disposed (or garbage collected, if a finalizer is implemented).
    /// Upon release, the next awaiting (or future) acquisition within the <see cref="ISyncContext.Manager"/>
    /// for the given <see cref="Context"/> will be allowed to proceed.
    /// </summary>
    /// <remarks>
    /// This synchronization scope is thread-independent.  Unlike "thread" synchronization models, this
    /// "context" synchronization model allows a scope to be acquired in one thread, but released within
    /// another allowing for asynchronous sub-tasks under a given scope without a blocking owner thread.
    /// In practice, a Finalizer should be implemented for any implementers of this interface to ensure
    /// any blocking threads are properly un-blocked.
    /// </remarks>
    public interface ISyncScope : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ISyncContext"/> under which this scope was acquired.
        /// </summary>
        ISyncContext Context { get; }

        /// <summary>
        /// Gets the synchronization root.
        /// </summary>
        object SyncRoot { get; }

        /// <summary>
        /// Gets a value indicating whether or not this instance has been disposed.
        /// </summary>
        /// <remarks>
        /// This should be checked when the lifetime of the <see cref="Context"/> cannot
        /// be guaranteed to be longer than the lifetime of this instance.  If true, best
        /// practice would be that no further modifications be made to the resources 
        /// synchronized by this scope.
        /// </remarks>
        bool IsDisposed { get; }
    }
}
