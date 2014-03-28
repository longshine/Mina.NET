using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Filter.Stream
{
    [TestClass]
    public abstract class AbstractStreamWriteFilterTest<M, U>
        where M : class
        where U : AbstractStreamWriteFilter<M>
    {
        [TestMethod]
        public void TestSetWriteBufferSize()
        {
            AbstractStreamWriteFilter<M> filter = CreateFilter();

            try
            {
                filter.WriteBufferSize = 0;
                Assert.Fail("0 writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            try
            {
                filter.WriteBufferSize = -100;
                Assert.Fail("Negative writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            filter.WriteBufferSize = 1;
            Assert.AreEqual(1, filter.WriteBufferSize);
            filter.WriteBufferSize = 1024;
            Assert.AreEqual(1024, filter.WriteBufferSize);
        }

        [TestMethod]
        public void TestWriteUsingSocketTransport()
        {
            AsyncSocketAcceptor acceptor = new AsyncSocketAcceptor();
            acceptor.ReuseAddress = true;
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 12345);

            AsyncSocketConnector connector = new AsyncSocketConnector();

            // Generate 4MB of random data
            Byte[] data = new Byte[4 * 1024 * 1024];
            new Random().NextBytes(data);

            Byte[] expectedMd5;
            using (MD5 md5 = MD5.Create())
            {
                expectedMd5 = md5.ComputeHash(data);
            }

            M message = CreateMessage(data);

            SenderHandler sender = new SenderHandler(message);
            ReceiverHandler receiver = new ReceiverHandler(data.Length);

            acceptor.Handler = sender;
            connector.Handler = receiver;

            acceptor.Bind(ep);
            connector.Connect(ep);
            sender.countdown.Wait();
            receiver.countdown.Wait();

            acceptor.Dispose();
            connector.Dispose();

            Assert.AreEqual(data.Length, receiver.ms.Length);
            Byte[] actualMd5;
            using (MD5 md5 = MD5.Create())
            {
                actualMd5 = md5.ComputeHash(receiver.ms.ToArray());
            }
            Assert.AreEqual(expectedMd5.Length, actualMd5.Length);
            for (Int32 i = 0; i < expectedMd5.Length; i++)
            {
                Assert.AreEqual(expectedMd5[i], actualMd5[i]);
            }
        }

        protected abstract AbstractStreamWriteFilter<M> CreateFilter();
        protected abstract M CreateMessage(Byte[] data);

        class SenderHandler : IoHandlerAdapter
        {
            M message;
            StreamWriteFilter streamWriteFilter = new StreamWriteFilter();
            public CountdownEvent countdown = new CountdownEvent(1);

            public SenderHandler(M m)
            {
                message = m;
            }

            public override void SessionCreated(IoSession session)
            {
                session.FilterChain.AddLast("codec", streamWriteFilter);
            }

            public override void SessionOpened(IoSession session)
            {
                session.Write(message);
            }

            public override void ExceptionCaught(IoSession session, Exception cause)
            {
                countdown.Signal();
            }

            public override void SessionClosed(IoSession session)
            {
                countdown.Signal();
            }

            public override void SessionIdle(IoSession session, IdleStatus status)
            {
                countdown.Signal();
            }

            public override void MessageSent(IoSession session, Object message)
            {
                if (message == this.message)
                {
                    countdown.Signal();
                }
            }
        }

        class ReceiverHandler : IoHandlerAdapter
        {
            Int64 size;
            public CountdownEvent countdown = new CountdownEvent(1);
            public MemoryStream ms = new MemoryStream();

            public ReceiverHandler(Int64 size)
            {
                this.size = size;
            }

            public override void SessionCreated(IoSession session)
            {
                session.Config.SetIdleTime(IdleStatus.ReaderIdle, 5);
            }

            public override void SessionIdle(IoSession session, IdleStatus status)
            {
                session.Close(true);
            }

            public override void ExceptionCaught(IoSession session, Exception cause)
            {
                countdown.Signal();
            }

            public override void SessionClosed(IoSession session)
            {
                countdown.Signal();
            }

            public override void MessageReceived(IoSession session, Object message)
            {
                IoBuffer buf = (IoBuffer)message;
                Byte[] bytes = new Byte[buf.Remaining];
                buf.Get(bytes, 0, bytes.Length);
                ms.Write(bytes, 0, bytes.Length);
                if (ms.Length >= size)
                    session.Close(true);
            }
        }
    }
}
