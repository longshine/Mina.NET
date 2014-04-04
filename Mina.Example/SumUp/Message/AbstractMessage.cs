using System;

namespace Mina.Example.SumUp.Message
{
    [Serializable]
    class AbstractMessage
    {
        public Int32 Sequence { get; set; }
    }
}
