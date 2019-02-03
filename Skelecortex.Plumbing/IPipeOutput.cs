namespace Skelecortex.Plumbing
{
    public interface IPipeOutput<out T>
    {
        IPipeReceiver<T> Flow (IFlowController controller);
    }
}
