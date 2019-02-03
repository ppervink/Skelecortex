using System;

namespace Skelecortex.Plumbing
{
    public interface IPipeReceiver<out T>
    {
        bool Received { get; }
        IFlow<T> GetFlow ();
        void WaitForFlow (Action onFlow);
    }
}
