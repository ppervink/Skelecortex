namespace Skelecortex.Plumbing
{
    public struct PipeReceiverTaskFlow<T> : IFlow<T>
    {
        public PipeReceiverTaskFlow (T content) => Content = content;

        public bool IsFlowing => true;

        public T Content { get; }
    }
}
