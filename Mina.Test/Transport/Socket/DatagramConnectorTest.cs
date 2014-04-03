using System;
using System.Net;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Future;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramConnectorTest : AbstractConnectorTest
    {
        protected override IoAcceptor CreateAcceptor()
        {
            return new AsyncDatagramAcceptor();
        }

        protected override IoConnector CreateConnector()
        {
            return new AsyncDatagramConnector();
        }

        protected override EndPoint CreateEndPoint(Int32 port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        public override void TestConnectFutureFailureTiming()
        {
            // Skip the test; Datagram connection can be made even if there's no
            // server at the endpoint.
        }
    }
}
