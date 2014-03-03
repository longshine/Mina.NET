using System;

namespace Mina.Transport.Socket
{
    public class AsyncSocketConnector : AbstractSocketConnector
    {
        protected override void BeginConnect(ConnectorContext connector)
        {
            connector.Socket.BeginConnect(connector.RemoteEP, ConnectCallback, connector);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            ConnectorContext connector = (ConnectorContext)ar.AsyncState;
            try
            {
                connector.Socket.EndConnect(ar);
            }
            catch (Exception ex)
            {
                EndConnect(ex, connector);
                return;
            }

            EndConnect(new AsyncSocketSession(this, Processor, connector.Socket, ReuseBuffer), connector);
        }
    }
}
