using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Provides <see cref="IProtocolEncoder"/> and <see cref="IProtocolDecoder"/> which translates
    /// binary or protocol specific data into message object and vice versa.
    /// </summary>
    public interface IProtocolCodecFactory
    {
        /// <summary>
        /// Returns a new (or reusable) instance of <see cref="IProtocolEncoder"/> which
        /// encodes message objects into binary or protocol-specific data.
        /// </summary>
        IProtocolEncoder GetEncoder(IoSession session);
        /// <summary>
        /// Returns a new (or reusable) instance of <see cref="IProtocolDecoder"/> which
        /// decodes binary or protocol-specific data into message objects.
        /// </summary>
        IProtocolDecoder GetDecoder(IoSession session);
    }
}
