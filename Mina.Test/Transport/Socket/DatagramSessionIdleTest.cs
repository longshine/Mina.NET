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
    public class DatagramSessionIdleTest
    {
        private Boolean readerIdleReceived;
        private Boolean writerIdleReceived;
        private Boolean bothIdleReceived;
        private Object mutex = new Object();

        [TestMethod]
        public void TestSessionIdle()
        {
            int READER_IDLE_TIME = 3;//seconds
            int WRITER_IDLE_TIME = READER_IDLE_TIME + 2;//seconds
            int BOTH_IDLE_TIME = WRITER_IDLE_TIME + 2;//seconds

            AsyncDatagramAcceptor acceptor = new AsyncDatagramAcceptor();
            acceptor.SessionConfig.SetIdleTime(IdleStatus.BothIdle, BOTH_IDLE_TIME);
            acceptor.SessionConfig.SetIdleTime(IdleStatus.ReaderIdle, READER_IDLE_TIME);
            acceptor.SessionConfig.SetIdleTime(IdleStatus.WriterIdle, WRITER_IDLE_TIME);
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 1234);
            acceptor.SessionIdle += (s, e) =>
            {
                if (e.IdleStatus == IdleStatus.BothIdle)
                {
                    bothIdleReceived = true;
                }
                else if (e.IdleStatus == IdleStatus.ReaderIdle)
                {
                    readerIdleReceived = true;
                }
                else if (e.IdleStatus == IdleStatus.WriterIdle)
                {
                    writerIdleReceived = true;
                }

                lock (mutex)
                {
                    System.Threading.Monitor.PulseAll(mutex);
                }
            };
            acceptor.Bind(ep);
            IoSession session = acceptor.NewSession(new IPEndPoint(IPAddress.Loopback, 1024), ep);

            //check properties to be copied from acceptor to session
            Assert.AreEqual(BOTH_IDLE_TIME, session.Config.BothIdleTime);
            Assert.AreEqual(READER_IDLE_TIME, session.Config.ReaderIdleTime);
            Assert.AreEqual(WRITER_IDLE_TIME, session.Config.WriterIdleTime);

            //verify that IDLE events really received by handler
            DateTime startTime = DateTime.Now;

            lock (mutex)
            {
                while (!readerIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (READER_IDLE_TIME + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(mutex, READER_IDLE_TIME * 1000);                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(readerIdleReceived);

            lock (mutex)
            {
                while (!writerIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (WRITER_IDLE_TIME + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(mutex, (WRITER_IDLE_TIME - READER_IDLE_TIME) * 1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(writerIdleReceived);

            lock (mutex)
            {
                while (!bothIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (BOTH_IDLE_TIME + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(mutex, (BOTH_IDLE_TIME - WRITER_IDLE_TIME) * 1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(bothIdleReceived);
        }
    }
}
