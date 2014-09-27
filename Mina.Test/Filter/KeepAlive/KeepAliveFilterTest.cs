using System;
using System.Net;
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
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Filter.KeepAlive
{
    [TestClass]
    public class KeepAliveFilterTest
    {
        static readonly IoBuffer PING = IoBuffer.Wrap(new Byte[] { 1 });
        static readonly IoBuffer PONG = IoBuffer.Wrap(new Byte[] { 2 });
        private static readonly Int32 INTERVAL = 5;
        private static readonly Int32 TIMEOUT = 1;

        private Int32 port;
        private AsyncSocketAcceptor acceptor;

        [TestInitialize]
        public void SetUp()
        {
            acceptor = new AsyncSocketAcceptor();
            IKeepAliveMessageFactory factory = new ServerFactory();
            KeepAliveFilter filter = new KeepAliveFilter(factory, IdleStatus.BothIdle);
            acceptor.FilterChain.AddLast("keep-alive", filter);
            acceptor.DefaultLocalEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            acceptor.Bind();
            port = ((IPEndPoint)acceptor.LocalEndPoint).Port;
        }

        [TestCleanup]
        public void TearDown()
        {
            acceptor.Unbind();
            acceptor.Dispose();
        }

        [TestMethod]
        public void TestKeepAliveFilterForReaderIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.ReaderIdle);
        }

        [TestMethod]
        public void TestKeepAliveFilterForWriterIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.WriterIdle);
        }

        [TestMethod]
        public void TestKeepAliveFilterForBothIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.BothIdle);
        }

        private void KeepAliveFilterForIdleStatus(IdleStatus status)
        {
            using (AsyncSocketConnector connector = new AsyncSocketConnector())
            {
                KeepAliveFilter filter = new KeepAliveFilter(new ClientFactory(), status, KeepAliveRequestTimeoutHandler.Exception, INTERVAL, TIMEOUT);
                filter.ForwardEvent = true;
                connector.FilterChain.AddLast("keep-alive", filter);

                Boolean gotException = false;
                connector.ExceptionCaught += (s, e) =>
                {
                    // A KeepAliveRequestTimeoutException will be thrown if no keep-alive response is received.
                    Console.WriteLine(e.Exception);
                    gotException = true;
                };

                IConnectFuture future = connector.Connect(new IPEndPoint(IPAddress.Loopback, port)).Await();
                IoSession session = future.Session;
                Assert.IsNotNull(session);

                Thread.Sleep((INTERVAL + TIMEOUT + 3) * 1000);

                Assert.IsFalse(gotException, "got an exception on the client");

                session.Close(true);
            }
        }

        static Boolean CheckRequest(IoBuffer message)
        {
            IoBuffer buff = message;
            Boolean check = buff.Get() == 1;
            buff.Rewind();
            return check;
        }

        static Boolean CheckResponse(IoBuffer message)
        {
            IoBuffer buff = message;
            Boolean check = buff.Get() == 2;
            buff.Rewind();
            return check;
        }

        class ServerFactory : IKeepAliveMessageFactory
        {
            public Object GetRequest(IoSession session)
            {
                return null;
            }

            public Object GetResponse(IoSession session, Object request)
            {
                return PONG.Duplicate();
            }

            public Boolean IsRequest(IoSession session, Object message)
            {
                if (message is IoBuffer)
                {
                    return CheckRequest((IoBuffer)message);
                }
                return false;
            }

            public Boolean IsResponse(IoSession session, Object message)
            {
                if (message is IoBuffer)
                {
                    return CheckResponse((IoBuffer)message);
                }
                return false;
            }
        }

        class ClientFactory : IKeepAliveMessageFactory
        {
            public Object GetRequest(IoSession session)
            {
                return PING.Duplicate();
            }

            public Object GetResponse(IoSession session, Object request)
            {
                return null;
            }

            public Boolean IsRequest(IoSession session, Object message)
            {
                if (message is IoBuffer)
                {
                    return CheckRequest((IoBuffer)message);
                }
                return false;
            }

            public Boolean IsResponse(IoSession session, Object message)
            {
                if (message is IoBuffer)
                {
                    return CheckResponse((IoBuffer)message);
                }
                return false;
            }
        }
    }
}
