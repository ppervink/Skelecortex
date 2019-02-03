using System;
using System.Diagnostics;

namespace Skelecortex.Synchronization
{
    internal sealed class SyncScope : ISyncScope
    {
        private readonly SyncContext _context;
        private volatile bool _isDisposed;

        internal SyncScope (SyncContext context, object syncRoot)
        {
            Debug.Assert(!(context is null || syncRoot is null));

            _context = context;
            SyncRoot = syncRoot;
        }

        public ISyncContext Context => _context;
        public object SyncRoot { get; }

        public bool IsDisposed =>
            _isDisposed || Context.IsDisposed;

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SyncScope () => Dispose(false);

        private void Dispose (bool disposing)
        {
            if (!_isDisposed)
            {
                // The below should be safe because:
                // Context is thread-safe, _isDisposed is volatile, and
                // this.IsDisposed is not referenced from Release
                // The only reason we checked _isDisposed first is to avoid the lock in Release,
                // but to let any race condition be resolved by that lock vs. introducing another
                _isDisposed = true;

                if (!disposing)
                {
                    // NOTE: In practice, this is not something that one should allow their code to do
                    // NOTE: May occur when closing/shutting the app down - will have to test/monitor to see
                    Trace.TraceWarning("A synchronization scope was allowed to be released by the garbage collector.");
                }

                _context.Release(SyncRoot);
            }
        }
    }
}
