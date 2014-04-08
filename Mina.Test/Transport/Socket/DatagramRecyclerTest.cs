using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramRecyclerTest
    {
        private AsyncDatagramAcceptor acceptor;
        private AsyncDatagramConnector connector;

        [TestInitialize]
        public void SetUp()
        {
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
        public void TestDatagramRecycler()
        {
            int port = 1024;
            ExpiringSessionRecycler recycler = new ExpiringSessionRecycler(1, 1);

            MockHandler acceptorHandler = new MockHandler();
            MockHandler connectorHandler = new MockHandler();

            acceptor.Handler = acceptorHandler;
            acceptor.SessionRecycler = recycler;
            acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                connector.Handler = connectorHandler;
                IConnectFuture future = connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                // Write whatever to trigger the acceptor.
                future.Session.Write(IoBuffer.Allocate(1)).Await();

                // Close the client-side connection.
                // This doesn't mean that the acceptor-side connection is also closed.
                // The life cycle of the acceptor-side connection is managed by the recycler.
                future.Session.Close(true);
                future.Session.CloseFuture.Await();
                Assert.IsTrue(future.Session.CloseFuture.Closed);

                // Wait until the acceptor-side connection is closed.
                while (acceptorHandler.session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.session.CloseFuture.Await(3000);

                // Is it closed?
                Assert.IsTrue(acceptorHandler.session.CloseFuture.Closed);

                Thread.Sleep(1000);

                Assert.AreEqual("CROPSECL", connectorHandler.result.ToString());
                Assert.AreEqual("CROPRECL", acceptorHandler.result.ToString());
            }
            finally
            {
                acceptor.Unbind();
            }
        }

        [TestMethod]
        public void TestCloseRequest()
        {
            int port = 1024;
            ExpiringSessionRecycler recycler = new ExpiringSessionRecycler(10, 1);

            MockHandler acceptorHandler = new MockHandler();
            MockHandler connectorHandler = new MockHandler();

            acceptor.SessionConfig.SetIdleTime(IdleStatus.ReaderIdle, 1);
            acceptor.Handler = acceptorHandler;
            acceptor.SessionRecycler = recycler;
            acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                connector.Handler = connectorHandler;
                IConnectFuture future = connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                // Write whatever to trigger the acceptor.
                future.Session.Write(IoBuffer.Allocate(1)).Await();

                // Make sure the connection is closed before recycler closes it.
                while (acceptorHandler.session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.session.Close(true);
                Assert.IsTrue(acceptorHandler.session.CloseFuture.Await(3000));

                IoSession oldSession = acceptorHandler.session;

                // Wait until all events are processed and clear the state.
                DateTime startTime = DateTime.Now;
                while (acceptorHandler.result.ToString().Length < 8)
                {
                    Thread.Yield();
                    if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                    {
                        throw new Exception();
                    }
                }
                acceptorHandler.result.Clear();
                acceptorHandler.session = null;

                // Write whatever to trigger the acceptor again.
                IWriteFuture wf = future.Session.Write(IoBuffer.Allocate(1)).Await();
                Assert.IsTrue(wf.Written);

                // Make sure the connection is closed before recycler closes it.
                while (acceptorHandler.session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.session.Close(true);
                Assert.IsTrue(acceptorHandler.session.CloseFuture.Await(3000));

                future.Session.Close(true).Await();

                Assert.AreNotSame(oldSession, acceptorHandler.session);
            }
            finally
            {
                acceptor.Unbind();
            }
        }

        class MockHandler : IoHandlerAdapter
        {
            public volatile IoSession session;

            public readonly StringBuilder result = new StringBuilder();

            public override void ExceptionCaught(IoSession session, Exception cause)
            {
                this.session = session;
                result.Append("CA");
            }

            public override void MessageReceived(IoSession session, Object message)
            {
                this.session = session;
                result.Append("RE");
            }

            public override void MessageSent(IoSession session, Object message)
            {
                this.session = session;
                result.Append("SE");
            }

            public override void SessionClosed(IoSession session)
            {
                this.session = session;
                result.Append("CL");
            }

            public override void SessionCreated(IoSession session)
            {
                this.session = session;
                result.Append("CR");
            }

            public override void SessionIdle(IoSession session, IdleStatus status)
            {
                this.session = session;
                result.Append("ID");
            }

            public override void SessionOpened(IoSession session)
            {
                this.session = session;
                result.Append("OP");
            }
        }
    }
}
