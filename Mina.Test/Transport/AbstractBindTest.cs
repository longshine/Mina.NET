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
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractBindTest
    {
        protected Int32 port;
        protected readonly IoAcceptor acceptor;

        public AbstractBindTest(IoAcceptor acceptor)
        {
            this.acceptor = acceptor;
        }

        [TestCleanup]
        public void TearDown()
        {
            acceptor.Unbind();
            acceptor.Dispose();
            acceptor.DefaultLocalEndPoint = null;
        }

        [TestMethod]
        public void TestAnonymousBind()
        {
            acceptor.DefaultLocalEndPoint = null;
            acceptor.Bind();
            Assert.IsNotNull(acceptor.LocalEndPoint);;
            acceptor.Unbind(acceptor.LocalEndPoint);
            Assert.IsNull(acceptor.LocalEndPoint);
            acceptor.DefaultLocalEndPoint = CreateEndPoint(0);
            acceptor.Bind();
            Assert.IsNotNull(acceptor.LocalEndPoint);
            Assert.IsTrue(GetPort(acceptor.LocalEndPoint) != 0);
            acceptor.Unbind(acceptor.LocalEndPoint);
        }

        [TestMethod]
        public void TestDuplicateBind()
        {
            Bind(false);

            try
            {
                acceptor.Bind();
                Assert.Fail("Exception is not thrown");
            }
            catch (Exception)
            {
                // Signifies a successfull test case execution
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void TestDuplicateUnbind()
        {
            Bind(false);

            // this should succeed
            acceptor.Unbind();

            // this shouldn't fail
            acceptor.Unbind();
        }

        [TestMethod]
        public void TestManyTimes()
        {
            Bind(true);

            for (Int32 i = 0; i < 1024; i++)
            {
                acceptor.Unbind();
                acceptor.Bind();
            }
        }

        [TestMethod]
        public void TestUnbindDisconnectsClients()
        {
            Bind(true);
            IoConnector connector = NewConnector();
            IoSession[] sessions = new IoSession[5];
            for (int i = 0; i < sessions.Length; i++)
            {
                IConnectFuture future = connector.Connect(CreateEndPoint(port));
                future.Await();
                sessions[i] = future.Session;
                Assert.IsTrue(sessions[i].Connected);
                Assert.IsTrue(sessions[i].Write(IoBuffer.Allocate(1)).Await().Written);
            }

            // Wait for the server side sessions to be created.
            Thread.Sleep(500);

            ICollection<IoSession> managedSessions = acceptor.ManagedSessions.Values;
            Assert.AreEqual(5, managedSessions.Count);

            acceptor.Unbind();

            // Wait for the client side sessions to close.
            Thread.Sleep(500);

            //Assert.AreEqual(0, managedSessions.Count);
            foreach (IoSession element in managedSessions)
            {
                Assert.IsFalse(element.Connected);
            }
        }

        [TestMethod]
        public void TestUnbindResume()
        {
            Bind(true);
            IoConnector connector = NewConnector();
            IoSession session = null;

            IConnectFuture future = connector.Connect(CreateEndPoint(port));
            future.Await();
            session = future.Session;
            Assert.IsTrue(session.Connected);
            Assert.IsTrue(session.Write(IoBuffer.Allocate(1)).Await().Written);

            // Wait for the server side session to be created.
            Thread.Sleep(500);

            ICollection<IoSession> managedSession = acceptor.ManagedSessions.Values;
            Assert.AreEqual(1, managedSession.Count);

            acceptor.Unbind();

            // Wait for the client side sessions to close.
            Thread.Sleep(500);

            //Assert.AreEqual(0, managedSession.Count);
            foreach (IoSession element in managedSession)
            {
                Assert.IsFalse(element.Connected);
            }

            // Rebind
            Bind(true);

            // Check again the connection
            future = connector.Connect(CreateEndPoint(port));
            future.Await();
            session = future.Session;
            Assert.IsTrue(session.Connected);
            Assert.IsTrue(session.Write(IoBuffer.Allocate(1)).Await().Written);

            // Wait for the server side session to be created.
            Thread.Sleep(500);

            managedSession = acceptor.ManagedSessions.Values;
            Assert.AreEqual(1, managedSession.Count);
        }

        protected abstract EndPoint CreateEndPoint(Int32 port);
        protected abstract Int32 GetPort(EndPoint ep);
        protected abstract IoConnector NewConnector();

        protected void Bind(Boolean reuseAddress)
        {
            acceptor.Handler = new EchoProtocolHandler();

            SetReuseAddress(reuseAddress);

            Boolean socketBound = false;
            for (port = 1024; port <= 65535; port++)
            {
                socketBound = false;
                try
                {
                    acceptor.DefaultLocalEndPoint = CreateEndPoint(port);
                    acceptor.Bind();
                    socketBound = true;
                    break;
                }
                catch (IOException)
                { }
            }

            if (!socketBound)
                throw new IOException("Cannot bind any test port.");
        }

        private void SetReuseAddress(Boolean reuseAddress)
        {
            if (acceptor is ISocketAcceptor)
            {
                ((ISocketAcceptor)acceptor).ReuseAddress = reuseAddress;
            }
        }

        class EchoProtocolHandler : IoHandlerAdapter
        {
            public override void SessionCreated(IoSession session)
            {
                session.Config.SetIdleTime(IdleStatus.BothIdle, 10);
            }

            public override void SessionIdle(IoSession session, IdleStatus status)
            {
                Console.WriteLine("*** IDLE #" + session.GetIdleCount(IdleStatus.BothIdle) + " ***");
            }

            public override void ExceptionCaught(IoSession session, Exception cause)
            {
                session.Close(true);
            }

            public override void MessageReceived(IoSession session, Object message)
            {
                IoBuffer rb = message as IoBuffer;
                if (rb == null)
                    return;

                // Write the received data back to remote peer
                IoBuffer wb = IoBuffer.Allocate(rb.Remaining);
                wb.Put(rb);
                wb.Flip();
                session.Write(wb);
            }
        }
    }
}
