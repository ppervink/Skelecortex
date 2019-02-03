using System;

namespace Skelecortex.Plumbing
{
    public interface IFlowStatus
    {
        bool IsFlowComplete { get; }
        void GetResult ();
        void OnFlowComplete (Action onFlowComplete);
    }
}
