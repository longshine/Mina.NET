namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A factory that creates a new instance of <see cref="IMessageEncoder"/>.
    /// </summary>
    public interface IMessageEncoderFactory
    {
        /// <summary>
        /// Creates a new message encoder.
        /// </summary>
        IMessageEncoder GetEncoder();
    }

    /// <summary>
    /// A factory that creates a new instance of <see cref="IMessageEncoder"/>.
    /// </summary>
    public interface IMessageEncoderFactory<T> : IMessageEncoderFactory
    {
        /// <summary>
        /// Creates a new message encoder.
        /// </summary>
        new IMessageEncoder<T> GetEncoder();
    }
}
