using System;
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
using Mina.Core.Session;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractTrafficControlTest
    {
        protected Int32 port;
        protected readonly IoAcceptor acceptor;

        public AbstractTrafficControlTest(IoAcceptor acceptor)
        {
            this.acceptor = acceptor;
        }

        [TestInitialize]
        public void SetUp()
        {
            acceptor.MessageReceived += (s, e) =>
            {
                // Just echo the received bytes.
                IoBuffer rb = (IoBuffer)e.Message;
                IoBuffer wb = IoBuffer.Allocate(rb.Remaining);
                wb.Put(rb);
                wb.Flip();
                e.Session.Write(wb);
            };
            acceptor.Bind(CreateServerEndPoint(0));
            port = GetPort(acceptor.LocalEndPoint);
        }

        [TestCleanup]
        public void TearDown()
        {
            acceptor.Unbind();
            acceptor.Dispose();
        }

        [TestMethod]
        public void TestSuspendResumeReadWrite()
        {
            IConnectFuture future = Connect(port, new ClientIoHandler());
            future.Await();
            IoSession session = future.Session;

            // We wait for the SessionCreated() event is fired because we
            // cannot guarantee that it is invoked already.
            while (session.GetAttribute("lock") == null)
            {
                Thread.Yield();
            }

            Object sync = session.GetAttribute("lock");
            lock (sync)
            {
                Write(session, "1");
                Assert.AreEqual('1', Read(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("1", GetSent(session));

                session.SuspendRead();

                Thread.Sleep(100);

                Write(session, "2");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.SuspendWrite();

                Thread.Sleep(100);

                Write(session, "3");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.ResumeRead();

                Thread.Sleep(100);

                Write(session, "4");
                Assert.AreEqual('2', Read(session));
                Assert.AreEqual("12", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.ResumeWrite();

                Thread.Sleep(100);

                Assert.AreEqual('3', Read(session));
                Assert.AreEqual('4', Read(session));

                Write(session, "5");
                Assert.AreEqual('5', Read(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("12345", GetSent(session));

                session.SuspendWrite();

                Thread.Sleep(100);

                Write(session, "6");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("12345", GetSent(session));

                session.SuspendRead();
                session.ResumeWrite();

                Thread.Sleep(100);

                Write(session, "7");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("1234567", GetSent(session));

                session.ResumeRead();

                Thread.Sleep(100);

                Assert.AreEqual('6', Read(session));
                Assert.AreEqual('7', Read(session));

                Assert.AreEqual("1234567", GetReceived(session));
                Assert.AreEqual("1234567", GetSent(session));
            }

            session.Close(true).Await();
        }

        protected abstract EndPoint CreateServerEndPoint(Int32 port);
        protected abstract Int32 GetPort(EndPoint ep);
        protected abstract IConnectFuture Connect(Int32 port, IoHandler handler);

        private void Write(IoSession session, String s)
        {
            session.Write(IoBuffer.Wrap(Encoding.ASCII.GetBytes(s)));
        }

        private Char Read(IoSession session)
        {
            Int32 pos = session.GetAttribute<Int32>("pos");
            for (Int32 i = 0; i < 10 && pos == GetReceived(session).Length; i++)
            {
                Object sync = session.GetAttribute("lock");
                lock (sync)
                {
                    Monitor.Wait(sync, 200);
                }
            }
            session.SetAttribute("pos", pos + 1);
            String received = GetReceived(session);
            Assert.IsTrue(received.Length > pos);
            return GetReceived(session)[pos];
        }

        private String GetReceived(IoSession session)
        {
            return session.GetAttribute("received").ToString();
        }

        private String GetSent(IoSession session)
        {
            return session.GetAttribute("sent").ToString();
        }

        private Boolean CanRead(IoSession session)
        {
            Int32 pos = session.GetAttribute<Int32>("pos");
            Object sync = session.GetAttribute("lock");
            lock (sync)
            {
                Monitor.Wait(sync, 250);
            }
            String received = GetReceived(session);
            return pos < received.Length;
        }

        class ClientIoHandler : IoHandlerAdapter
        {
            public override void SessionCreated(IoSession session)
            {
                session.SetAttribute("pos", 0);
                session.SetAttribute("received", new StringBuilder());
                session.SetAttribute("sent", new StringBuilder());
                session.SetAttribute("lock", new Object());
            }

            public override void MessageReceived(IoSession session, Object message)
            {
                IoBuffer buffer = (IoBuffer)message;
                Byte[] data = new Byte[buffer.Remaining];
                buffer.Get(data, 0, data.Length);
                Object sync = session.GetAttribute("lock");
                lock (sync)
                {
                    StringBuilder sb = session.GetAttribute<StringBuilder>("received");
                    sb.Append(Encoding.ASCII.GetString(data));
                    Monitor.PulseAll(sync);
                }
            }

            public override void MessageSent(IoSession session, Object message)
            {
                IoBuffer buffer = (IoBuffer)message;
                buffer.Rewind();
                Byte[] data = new Byte[buffer.Remaining];
                buffer.Get(data, 0, data.Length);
                StringBuilder sb = session.GetAttribute<StringBuilder>("sent");
                sb.Append(Encoding.ASCII.GetString(data));
            }
        }
    }
}
