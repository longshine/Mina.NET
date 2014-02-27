using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which consumes all bytes until a fixed (ASCII) 
    /// character is reached. The terminator is skipped.
    /// </summary>
    public abstract class ConsumeToTerminatorDecodingState : IDecodingState
    {
        private readonly Byte _terminator;
        private IoBuffer _buffer;

        /// <summary>
        /// Creates a new instance using the specified terminator character.
        /// </summary>
        /// <param name="terminator">the terminator character</param>
        protected ConsumeToTerminatorDecodingState(Byte terminator)
        {
            _terminator = terminator;
        }

        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            Int32 terminatorPos = input.IndexOf(_terminator);

            if (terminatorPos >= 0)
            {
                Int32 limit = input.Limit;
                IoBuffer product;

                if (input.Position < terminatorPos)
                {
                    input.Limit = terminatorPos;

                    if (_buffer == null)
                    {
                        product = input.Slice();
                    }
                    else
                    {
                        _buffer.Put(input);
                        product = _buffer.Flip();
                        _buffer = null;
                    }

                    input.Limit = limit;
                }
                else
                {
                    // When input contained only terminator rather than actual data...
                    if (_buffer == null)
                    {
                        product = IoBuffer.Allocate(0);
                    }
                    else
                    {
                        product = _buffer.Flip();
                        _buffer = null;
                    }
                }
                input.Position = terminatorPos + 1;
                return FinishDecode(product, output);
            }

            if (_buffer == null)
            {
                _buffer = IoBuffer.Allocate(input.Remaining);
                _buffer.AutoExpand = true;
            }

            _buffer.Put(input);
            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            IoBuffer product;
            // When input contained only terminator rather than actual data...
            if (_buffer == null)
            {
                product = IoBuffer.Allocate(0);
            }
            else
            {
                product = _buffer.Flip();
                _buffer = null;
            }
            return FinishDecode(product, output);
        }

        /// <summary>
        /// Invoked when this state has reached the terminator byte.
        /// </summary>
        /// <param name="product">the read bytes not including the terminator</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(IoBuffer product, IProtocolDecoderOutput output);
    }
}
