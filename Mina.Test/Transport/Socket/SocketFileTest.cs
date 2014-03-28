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
    public class SocketFileTest : AbstractFileTest
    {
        protected override IoAcceptor CreateAcceptor()
        {
            return new AsyncSocketAcceptor();
        }

        protected override IoConnector CreateConnector()
        {
            return new AsyncSocketConnector();
        }

        protected override EndPoint CreateEndPoint(Int32 port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }
    }
}
