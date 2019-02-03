using System;

namespace Skelecortex.Synchronization
{
    public interface ISyncManager
    {
        ISyncScope Acquire (object syncRoot);
        ISyncContext CreateContext ();
        bool TryAcquire (object syncRoot, int timeout, out ISyncScope syncScope);
    }
}