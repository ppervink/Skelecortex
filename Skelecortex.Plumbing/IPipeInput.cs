namespace Skelecortex.Plumbing
{
    public interface IPipeInput<in T>
    {
        IFlowStatus Flow (T content, IFlowController controller);
    }
}
