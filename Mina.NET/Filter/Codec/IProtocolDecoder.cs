using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Decodes binary or protocol-specific data into higher-level message objects.
    /// </summary>
    public interface IProtocolDecoder
    {
        /// <summary>
        /// Decodes binary or protocol-specific data into higher-level message objects.
        /// </summary>
        void Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output);
        /// <summary>
        /// Invoked when the specified <tt>session</tt> is closed.  This method is useful
        /// when you deal with the protocol which doesn't specify the length of a message
        /// such as HTTP response without <tt>content-length</tt> header.
        /// </summary>
        void FinishDecode(IoSession session, IProtocolDecoderOutput output);
        /// <summary>
        /// Releases all resources related with this decoder.
        /// </summary>
        void Dispose(IoSession session);
    }
}
