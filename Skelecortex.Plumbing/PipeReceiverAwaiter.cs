using System;
using System.Runtime.CompilerServices;

namespace Skelecortex.Plumbing
{
    public readonly struct PipeReceiverAwaiter<TContent> : INotifyCompletion
    {
        public IPipeReceiver<TContent> Receiver { get; }

        public bool IsCompleted => Receiver.Received;

        public IFlow<TContent> GetResult () => Receiver.GetFlow();

        public PipeReceiverAwaiter (IPipeReceiver<TContent> receiver)
        {
            Receiver = receiver;
        }

        public void OnCompleted (Action continuation)
        {
            Receiver.WaitForFlow(continuation);
        }
    }
}
