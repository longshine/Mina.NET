using System;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class ResultMessageDecoder : AbstractMessageDecoder
    {
        private Int32 _code;
        private Boolean _readCode;

        public ResultMessageDecoder()
            : base(Constants.RESULT)
        { }

        protected override AbstractMessage DecodeBody(IoSession session, IoBuffer input)
        {
            if (!_readCode)
            {
                if (input.Remaining < Constants.RESULT_CODE_LEN)
                {
                    return null; // Need more data.
                }

                _code = input.GetInt16();
                _readCode = true;
            }

            if (_code == Constants.RESULT_OK)
            {
                if (input.Remaining < Constants.RESULT_VALUE_LEN)
                {
                    return null;
                }

                ResultMessage m = new ResultMessage();
                m.OK = true;
                m.Value = input.GetInt32();
                _readCode = false;
                return m;
            }
            else
            {
                ResultMessage m = new ResultMessage();
                m.OK = false;
                _readCode = false;
                return m;
            }
        }
    }
}
