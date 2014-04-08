using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Service;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramConfigTest
    {
        private IoAcceptor acceptor;
        private IoConnector connector;
        static String result;

        [TestInitialize]
        public void SetUp()
        {
            result = "";
            acceptor = new AsyncDatagramAcceptor();
            connector = new AsyncDatagramConnector();
        }

        [TestCleanup]
        public void TearDown()
        {
            acceptor.Dispose();
            connector.Dispose();
        }

        [TestMethod]
        public void TestAcceptorFilterChain()
        {
            Int32 port = 1024;
            IoFilter mockFilter = new MockFilter();
            IoHandler mockHandler = new MockHandler();

            acceptor.FilterChain.AddLast("mock", mockFilter);
            acceptor.Handler = mockHandler;
            acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                IConnectFuture future = connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                IWriteFuture writeFuture = future.Session.Write(IoBuffer.Allocate(16).PutInt32(0).Flip());
                writeFuture.Await();
                Assert.IsTrue(writeFuture.Written);

                future.Session.Close(true);

                for (int i = 0; i < 30; i++)
                {
                    if (result.Length == 2)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }

                Assert.AreEqual("FH", result);
            }
            finally
            {
                acceptor.Unbind();
            }
        }

        class MockFilter : IoFilterAdapter
        {
            public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
            {
                result += "F";
                nextFilter.MessageReceived(session, message);
            }
        }

        class MockHandler : IoHandlerAdapter
        {
            public override void MessageReceived(IoSession session, Object message)
            {
                result += "H";
            }
        }
    }
}
