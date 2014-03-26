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
    public class SocketBindTest : AbstractBindTest
    {
        public SocketBindTest()
            : base(new AsyncSocketAcceptor())
        { }

        protected override EndPoint CreateEndPoint(Int32 port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        protected override Int32 GetPort(EndPoint ep)
        {
            return ((IPEndPoint)ep).Port;
        }

        protected override IoConnector NewConnector()
        {
            return new AsyncSocketConnector();
        }
    }
}
