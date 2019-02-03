namespace Skelecortex.Plumbing
{
    public interface IFlowController
    {
        bool Flow<TContent> (IFlow<TContent> flow);
        bool IsFlowing { get; }
    }
}
