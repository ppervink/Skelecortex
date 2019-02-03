using System;
using System.Runtime.CompilerServices;

namespace Skelecortex.Plumbing
{
    //TODO: Extensions to create this? (quesion that)

    public readonly struct FlowStatusAwaiter : INotifyCompletion
    {
        public IFlowStatus Status { get; }

        public FlowStatusAwaiter (IFlowStatus flowStatus)
        {
            Status = flowStatus;
        }

        public bool IsCompleted => Status.IsFlowComplete;

        public void GetResult ()=>
            Status.GetResult();

        public void OnCompleted (Action continuation)
        {
            Status.OnFlowComplete(continuation);
        }
    }

    // TODO: Pipeline builder with fluid API (i.e. pipeline.OutputTo(next).OutputTo(another) where OutputTo is generic
}
