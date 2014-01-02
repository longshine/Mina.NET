using System;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which consumes all bytes until a space (0x20) or tab 
    /// (0x09) character is reached. The terminator is skipped.
    /// </summary>
    public abstract class ConsumeToLinearWhitespaceDecodingState : ConsumeToDynamicTerminatorDecodingState
    {
        protected override Boolean IsTerminator(Byte b)
        {
            return (b == ' ') || (b == '\t');
        }
    }
}
