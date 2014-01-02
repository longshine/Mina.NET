using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which consumes all bytes until a fixed (ASCII) 
    /// character is reached. The terminator is skipped.
    /// </summary>
    public abstract class ConsumeToDynamicTerminatorDecodingState : IDecodingState
    {
        private IoBuffer _buffer;

        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            Int32 beginPos = input.Position;
            Int32 terminatorPos = -1;
            Int32 limit = input.Limit;

            for (Int32 i = beginPos; i < limit; i++)
            {
                Byte b = input.Get(i);
                if (IsTerminator(b))
                {
                    terminatorPos = i;
                    break;
                }
            }

            if (terminatorPos >= 0)
            {
                IoBuffer product;

                if (beginPos < terminatorPos)
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
        /// Determines whether the specified <code>byte</code> is a terminator.
        /// </summary>
        /// <param name="b">the <code>byte</code> to check</param>
        /// <returns><code>true</code> if <code>b</code> is a terminator, otherwise false</returns>
        protected abstract Boolean IsTerminator(Byte b);

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
