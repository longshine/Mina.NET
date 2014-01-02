using System;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Demux;

namespace Mina.Example.SumUp.Codec
{
    abstract class AbstractMessageDecoder : IMessageDecoder
    {
        private readonly Int32 _type;
        private Int32 _sequence;
        private Boolean _readHeader;

        public AbstractMessageDecoder(Int32 type)
        {
            _type = type;
        }

        public MessageDecoderResult Decodable(IoSession session, IoBuffer input)
        {
            // Return NeedData if the whole header is not read yet.
            if (input.Remaining < Constants.HEADER_LEN)
                return MessageDecoderResult.NeedData;

            // Return OK if type and bodyLength matches.
            if (_type == input.GetInt16())
                return MessageDecoderResult.OK;

            // Return NotOK if not matches.
            return MessageDecoderResult.NotOK;
        }

        public MessageDecoderResult Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            // Try to skip header if not read.
            if (!_readHeader)
            {
                input.GetInt16(); // Skip 'type'.
                _sequence = input.GetInt32(); // Get 'sequence'.
                _readHeader = true;
            }

            // Try to decode body
            AbstractMessage m = DecodeBody(session, input);
            // Return NEED_DATA if the body is not fully read.
            if (m == null)
            {
                return MessageDecoderResult.NeedData;
            }
            else
            {
                _readHeader = false; // reset readHeader for the next decode
            }
            m.Sequence = _sequence;
            output.Write(m);

            return MessageDecoderResult.OK;
        }

        public virtual void FinishDecode(IoSession session, IProtocolDecoderOutput output)
        { }

        protected abstract AbstractMessage DecodeBody(IoSession session, IoBuffer input);
    }
}
