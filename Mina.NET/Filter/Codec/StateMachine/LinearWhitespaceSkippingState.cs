using System;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which skips space (0x20) and tab (0x09) characters.
    /// </summary>
    public abstract class LinearWhitespaceSkippingState : SkippingState
    {
        protected override Boolean CanSkip(Byte b)
        {
            return b == 32 || b == 9;
        }
    }
}
