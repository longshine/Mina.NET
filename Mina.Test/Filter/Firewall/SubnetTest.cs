using System;
using System.Net;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Mina.Filter.Firewall
{
    [TestClass]
    public class SubnetTest
    {
        [TestMethod]
        public void TestIPv6()
        {
            IPAddress addr = IPAddress.Parse("1080:0:0:0:8:800:200C:417A");

            Assert.IsTrue(addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

            new Subnet(addr, 24);
        }

        [TestMethod]
        public void Test24()
        {
            IPAddress a = IPAddress.Parse("127.2.3.0");
            IPAddress b = IPAddress.Parse("127.2.3.4");
            IPAddress c = IPAddress.Parse("127.2.3.255");
            IPAddress d = IPAddress.Parse("127.2.4.4");

            Subnet mask = new Subnet(a, 24);

            Assert.IsTrue(mask.InSubnet(a));
            Assert.IsTrue(mask.InSubnet(b));
            Assert.IsTrue(mask.InSubnet(c));
            Assert.IsFalse(mask.InSubnet(d));
        }

        [TestMethod]
        public void Test16()
        {
            IPAddress a = IPAddress.Parse("127.2.0.0");
            IPAddress b = IPAddress.Parse("127.2.3.4");
            IPAddress c = IPAddress.Parse("127.2.129.255");
            IPAddress d = IPAddress.Parse("127.3.4.4");

            Subnet mask = new Subnet(a, 16);

            Assert.IsTrue(mask.InSubnet(a));
            Assert.IsTrue(mask.InSubnet(b));
            Assert.IsTrue(mask.InSubnet(c));
            Assert.IsFalse(mask.InSubnet(d));
        }

        [TestMethod]
        public void TestSingleIp()
        {
            IPAddress a = IPAddress.Parse("127.2.3.4");
            IPAddress b = IPAddress.Parse("127.2.3.3");
            IPAddress c = IPAddress.Parse("127.2.3.255");
            IPAddress d = IPAddress.Parse("127.2.3.0");

            Subnet mask = new Subnet(a, 32);

            Assert.IsTrue(mask.InSubnet(a));
            Assert.IsFalse(mask.InSubnet(b));
            Assert.IsFalse(mask.InSubnet(c));
            Assert.IsFalse(mask.InSubnet(d));
        }

        [TestMethod]
        public void TestToString()
        {
            IPAddress a = IPAddress.Parse("127.2.3.0");
            Subnet mask = new Subnet(a, 24);

            Assert.AreEqual("127.2.3.0/24", mask.ToString());
        }

        [TestMethod]
        public void TestToStringLiteral()
        {
            IPAddress a = IPAddress.Loopback;
            Subnet mask = new Subnet(a, 32);

            Assert.AreEqual("127.0.0.1/32", mask.ToString());
        }

        [TestMethod]
        public void TestEquals()
        {
            Subnet a = new Subnet(IPAddress.Parse("127.2.3.4"), 32);
            Subnet b = new Subnet(IPAddress.Parse("127.2.3.4"), 32);
            Subnet c = new Subnet(IPAddress.Parse("127.2.3.5"), 32);
            Subnet d = new Subnet(IPAddress.Parse("127.2.3.5"), 24);

            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsFalse(a.Equals(d));
            Assert.IsFalse(a.Equals(null));
        }
    }
}
