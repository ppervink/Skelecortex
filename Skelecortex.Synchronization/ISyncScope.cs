using System;

namespace Skelecortex.Synchronization
{
    public interface ISyncScope : IDisposable
    {
        ISyncContext Context { get; }
        bool IsDisposed { get; }
    }
}
