using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class AddMessageDecoder : AbstractMessageDecoder
    {
        public AddMessageDecoder()
            : base(Constants.ADD)
        { }

        protected override Message.AbstractMessage DecodeBody(IoSession session, IoBuffer input)
        {
            if (input.Remaining < Constants.ADD_BODY_LEN)
            {
                return null;
            }

            AddMessage m = new AddMessage();
            m.Value = input.GetInt32();
            return m;
        }
    }
}
