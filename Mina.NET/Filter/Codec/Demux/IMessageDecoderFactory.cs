namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A factory that creates a new instance of <see cref="IMessageDecoder"/>.
    /// </summary>
    public interface IMessageDecoderFactory
    {
        /// <summary>
        /// Creates a new message decoder.
        /// </summary>
        IMessageDecoder GetDecoder();
    }
}
