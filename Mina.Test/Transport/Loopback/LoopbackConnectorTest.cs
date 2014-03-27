using System;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Future;
using Mina.Core.Service;

namespace Mina.Transport.Loopback
{
    [TestClass]
    public class LoopbackConnectorTest : AbstractConnectorTest
    {
        protected override IoAcceptor CreateAcceptor()
        {
            return new LoopbackAcceptor();
        }

        protected override IoConnector CreateConnector()
        {
            return new LoopbackConnector();
        }

        protected override System.Net.EndPoint CreateEndPoint(Int32 port)
        {
            return new LoopbackEndPoint(port);
        }
    }
}
