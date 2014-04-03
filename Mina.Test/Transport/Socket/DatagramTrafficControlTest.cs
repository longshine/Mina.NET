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
    public class DatagramTrafficControlTest : AbstractTrafficControlTest
    {
        public DatagramTrafficControlTest()
            : base(new AsyncDatagramAcceptor())
        { }

        protected override EndPoint CreateServerEndPoint(Int32 port)
        {
            return new IPEndPoint(IPAddress.Any, port);
        }

        protected override Int32 GetPort(EndPoint ep)
        {
            return ((IPEndPoint)ep).Port;
        }

        protected override IConnectFuture Connect(Int32 port, IoHandler handler)
        {
            IoConnector connector = new AsyncDatagramConnector();
            connector.Handler = handler;
            return connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
        }
    }
}
