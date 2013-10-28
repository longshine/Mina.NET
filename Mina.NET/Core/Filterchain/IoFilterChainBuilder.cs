namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An interface that builds <see cref="IoFilterChain"/> in predefined way
    /// when <see cref="IoSession"/> is created.
    /// </summary>
    public interface IoFilterChainBuilder
    {
        void BuildFilterChain(IoFilterChain chain);
    }
}
