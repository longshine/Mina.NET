using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    ///  <see cref="IDecodingState"/> which consumes all received bytes until a configured
    ///  number of read bytes has been reached. Please note that this state can
    ///  produce a buffer with less data than the configured length if the associated 
    ///  session has been closed unexpectedly.
    /// </summary>
    public abstract class FixedLengthDecodingState : IDecodingState
    {
        private readonly Int32 _length;
        private IoBuffer _buffer;

        /// <summary>
        /// Constructs a new instance using the specified decode length.
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        protected FixedLengthDecodingState(Int32 length)
        {
            _length = length;
        }

        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            if (_buffer == null)
            {
                if (input.Remaining >= _length)
                {
                    Int32 limit = input.Limit;
                    input.Limit = input.Position + _length;
                    IoBuffer product = input.Slice();
                    input.Position = input.Position + _length;
                    input.Limit = limit;
                    return FinishDecode(product, output);
                }

                _buffer = IoBuffer.Allocate(_length);
                _buffer.Put(input);
                return this;
            }

            if (input.Remaining >= _length - _buffer.Position)
            {
                Int32 limit = input.Limit;
                input.Limit = input.Position + _length - _buffer.Position;
                _buffer.Put(input);
                input.Limit = limit;
                IoBuffer product = _buffer;
                _buffer = null;
                return FinishDecode(product.Flip(), output);
            }

            _buffer.Put(input);
            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            IoBuffer readData;
            if (_buffer == null)
            {
                readData = IoBuffer.Allocate(0);
            }
            else
            {
                readData = _buffer.Flip();
                _buffer = null;
            }
            return FinishDecode(readData, output);
        }

        /// <summary>
        /// Invoked when this state has consumed the configured number of bytes.
        /// </summary>
        /// <param name="product">the data</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(IoBuffer product, IProtocolDecoderOutput output);
    }
}
