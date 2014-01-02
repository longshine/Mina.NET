using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class ResultMessageEncoder<T> : AbstractMessageEncoder<T>
        where T : ResultMessage
    {
        public ResultMessageEncoder()
            : base(Constants.RESULT)
        { }

        protected override void EncodeBody(IoSession session, T message, IoBuffer output)
        {
            if (message.OK)
            {
                output.PutInt16((short)Constants.RESULT_OK);
                output.PutInt32(message.Value);
            }
            else
            {
                output.PutInt16((short)Constants.RESULT_ERROR);
            }
        }
    }
}
