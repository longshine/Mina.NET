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
    public class LoopbackBindTest : AbstractBindTest
    {
        public LoopbackBindTest()
            : base(new LoopbackAcceptor())
        { }

        protected override System.Net.EndPoint CreateEndPoint(Int32 port)
        {
            return new LoopbackEndPoint(port);
        }

        protected override Int32 GetPort(System.Net.EndPoint ep)
        {
            return ((LoopbackEndPoint)ep).Port;
        }

        protected override IoConnector NewConnector()
        {
            return new LoopbackConnector();
        }
    }
}
