using System;

namespace Mina.Example.SumUp.Message
{
    class AddMessage : AbstractMessage
    {
        public Int32 Value { get; set; }

        public override String ToString()
        {
            return Sequence + ":ADD(" + Value + ')';
        }
    }
}
