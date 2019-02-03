namespace Skelecortex.Plumbing
{
    public interface IFlow<out TContent>
    {
        bool IsFlowing { get; }
        TContent Content { get; }
    }
}
