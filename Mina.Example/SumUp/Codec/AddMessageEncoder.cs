using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class AddMessageEncoder<T> : AbstractMessageEncoder<T>
        where T : AddMessage
    {
        public AddMessageEncoder()
            : base(Constants.ADD)
        { }

        protected override void EncodeBody(IoSession session, T message, IoBuffer output)
        {
            output.PutInt32(message.Value);
        }
    }
}
