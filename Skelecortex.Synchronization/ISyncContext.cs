using System;

namespace Skelecortex.Synchronization
{
    public interface ISyncContext : IDisposable
    {
        bool IsDisposed { get; }
        ISyncManager Manager { get; }

        ISyncScope Acquire (object syncRoot);
        bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope);
    }
}