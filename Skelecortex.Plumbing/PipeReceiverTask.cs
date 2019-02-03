using System;
using System.Runtime.CompilerServices;

namespace Skelecortex.Plumbing
{
    public struct PipeReceiverTask<T> : IPipeReceiver<T>
    {
        private readonly TaskAwaiter<T> _awaiter;

        public PipeReceiverTask (TaskAwaiter<T> awaiter) => _awaiter = awaiter;

        public bool Received => _awaiter.IsCompleted;

        public IFlow<T> GetFlow () => new PipeReceiverTaskFlow<T>(_awaiter.GetResult());

        public void WaitForFlow (Action onFlow) => _awaiter.OnCompleted(onFlow);
    }
}
