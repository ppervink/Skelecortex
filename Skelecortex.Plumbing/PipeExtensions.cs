using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Skelecortex.Plumbing
{
    public static class PipeExtensions
    {
        public static PipeReceiverAwaiter<TContent> GetAwaiter<TContent> (this IPipeReceiver<TContent> receiver)
        {
            if (receiver is null) throw new ArgumentNullException(nameof(receiver));
            return new PipeReceiverAwaiter<TContent>(receiver);
        }

        public static PipeReceiverTask<T> AsPipeReceiverTask<T> (this TaskAwaiter<T> awaiter) =>
            new PipeReceiverTask<T>(awaiter);

        public static PipeReceiverTask<T> AsPipeReceiverTask<T> (this Task<T> task, bool continueOnCapturedContext = true) =>
            task?.GetAwaiter().AsPipeReceiverTask() ?? throw new ArgumentNullException(nameof(task));
        
        public static FlowStatusAwaiter GetAwaiter (this IFlowStatus flowStatus) =>
            new FlowStatusAwaiter(flowStatus);
    }
}
