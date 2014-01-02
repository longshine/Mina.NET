using System;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Demux;

namespace Mina.Example.SumUp.Codec
{
    abstract class AbstractMessageEncoder<T> : IMessageEncoder<T>
        where T : AbstractMessage
    {
        private readonly Int32 _type;

        protected AbstractMessageEncoder(Int32 type)
        {
            _type = type;
        }

        public void Encode(IoSession session, T message, IProtocolEncoderOutput output)
        {
            IoBuffer buf = IoBuffer.Allocate(16);
            buf.AutoExpand = true; // Enable auto-expand for easier encoding

            // Encode a header
            buf.PutInt16((short)_type);
            buf.PutInt32(message.Sequence);

            // Encode a body
            EncodeBody(session, message, buf);
            buf.Flip();
            output.Write(buf);
        }

        public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            Encode(session, (T)message, output);
        }

        protected abstract void EncodeBody(IoSession session, T message, IoBuffer output);
    }
}
