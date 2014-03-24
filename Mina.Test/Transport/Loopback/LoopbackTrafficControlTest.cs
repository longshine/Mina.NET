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
    public class LoopbackTrafficControlTest : AbstractTrafficControlTest
    {
        public LoopbackTrafficControlTest()
            : base(new LoopbackAcceptor())
        { }

        protected override System.Net.EndPoint CreateServerEndPoint(Int32 port)
        {
            return new LoopbackEndPoint(port);
        }

        protected override Int32 GetPort(System.Net.EndPoint ep)
        {
            return ((LoopbackEndPoint)ep).Port;
        }

        protected override IConnectFuture Connect(Int32 port, IoHandler handler)
        {
            IoConnector connector = new LoopbackConnector();
            connector.Handler = handler;
            return connector.Connect(new LoopbackEndPoint(port));
        }
    }
}
