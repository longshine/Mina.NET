using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// Decodes a certain type of messages.
    /// </summary>
    public interface IMessageDecoder
    {
        /// <summary>
        /// Checks the specified buffer is decodable by this decoder.
        /// </summary>
        /// <returns>
        /// <see cref="MessageDecoderResult.OK"/> if this decoder can decode the specified buffer.
        /// <see cref="MessageDecoderResult.NotOK"/> if this decoder cannot decode the specified buffer.
        /// <see cref="MessageDecoderResult.NeedData"/> if more data is required to determine if the specified buffer is decodable or not.
        /// </returns>
        MessageDecoderResult Decodable(IoSession session, IoBuffer input);

        /// <summary>
        /// Decodes binary or protocol-specific content into higher-level message objects.
        /// </summary>
        /// <remarks>
        /// MINA invokes <code>Decode(IoSession, IoBuffer, ProtocolDecoderOutput)</code>
        /// method with read data, and then the decoder implementation puts decoded
        /// messages into <see cref="IProtocolDecoderOutput"/>.
        /// </remarks>
        /// <returns>
        /// <see cref="MessageDecoderResult.OK"/> if you finished decoding messages successfully.
        /// <see cref="MessageDecoderResult.NeedData"/> if you need more data to finish decoding current message.
        /// <see cref="MessageDecoderResult.NotOK"/> if you cannot decode current message due to protocol specification violation.
        /// </returns>
        MessageDecoderResult Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output);

        /// <summary>
        /// Invoked when the specified <code>session</code> is closed while this decoder was
        /// parsing the data.  This method is useful when you deal with the protocol which doesn't
        /// specify the length of a message such as HTTP response without <code>content-length</code>
        /// header. Implement this method to process the remaining data that
        /// <code>Decode(IoSession, IoBuffer, ProtocolDecoderOutput)</code> method didn't process completely.
        /// </summary>
        void FinishDecode(IoSession session, IProtocolDecoderOutput output);
    }
}
