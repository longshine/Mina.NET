namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An interface that builds <see cref="IoFilterChain"/> in predefined way
    /// when <see cref="Core.Session.IoSession"/> is created.
    /// </summary>
    public interface IoFilterChainBuilder
    {
        /// <summary>
        /// Builds the specified <paramref name="chain"/>.
        /// </summary>
        /// <param name="chain">the chain to build</param>
        void BuildFilterChain(IoFilterChain chain);
    }
}
