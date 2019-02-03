using System;
using System.Runtime.CompilerServices;

namespace Skelecortex.Plumbing
{
    public readonly struct FlowStatusTask : IFlowStatus
    {
        private readonly TaskAwaiter _awaiter;

        public FlowStatusTask(TaskAwaiter awaiter)
        {
            _awaiter = awaiter;
        }

        public bool IsFlowComplete => 
            _awaiter.IsCompleted;

        public void GetResult ()=>
            _awaiter.GetResult();

        public void OnFlowComplete (Action onFlowComplete)=>
            _awaiter.OnCompleted(onFlowComplete);
    }
}
