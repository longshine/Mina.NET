using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// An abstract <see cref="IMessageDecoder"/> implementation for those who don't need to
    /// implement <code>FinishDecode(IoSession, IProtocolDecoderOutput)</code> method.
    /// </summary>
    public abstract class MessageDecoderAdapter : IMessageDecoder
    {
        public abstract MessageDecoderResult Decodable(IoSession session, IoBuffer input);

        public abstract MessageDecoderResult Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output);

        public virtual void FinishDecode(IoSession session, IProtocolDecoderOutput output)
        {
            // Do nothing
        }
    }
}
