using System;

namespace Mina.Example.SumUp.Message
{
    [Serializable]
    class ResultMessage : AbstractMessage
    {
        public Boolean OK { get; set; }

        public Int32 Value { get; set; }

        public override String ToString()
        {
            return Sequence + (OK ? ":RESULT(" + Value + ')' : ":RESULT(ERROR)");
        }
    }
}
