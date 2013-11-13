using System.Net;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Session;

namespace Mina.Filter.Firewall
{
    [TestClass]
    public class ConnectionThrottleFilterTest
    {
        private ConnectionThrottleFilter filter = new ConnectionThrottleFilter();
        private DummySession sessionOne = new DummySession();
        private DummySession sessionTwo = new DummySession();

        public ConnectionThrottleFilterTest()
        {
            sessionOne.SetRemoteEndPoint(new IPEndPoint(IPAddress.Any, 1234));
            sessionTwo.SetRemoteEndPoint(new IPEndPoint(IPAddress.Any, 1235));
        }

        [TestMethod]
        public void TestGoodConnection()
        {
            filter.AllowedInterval = 100;
            filter.IsConnectionOk(sessionOne);

            Thread.Sleep(1000);

            Assert.IsTrue(filter.IsConnectionOk(sessionOne));
        }

        [TestMethod]
        public void TestBadConnection()
        {
            filter.AllowedInterval = 1000;
            filter.IsConnectionOk(sessionTwo);
            Assert.IsFalse(filter.IsConnectionOk(sessionTwo));
        }
    }
}
