using System;
using System.IO;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core
{
    [TestClass]
    public class FutureTest
    {
        [TestMethod]
        public void TestCloseFuture()
        {
            DefaultCloseFuture future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);
        }

        [TestMethod]
        public void TestConnectFuture()
        {
            DefaultConnectFuture future = new DefaultConnectFuture();
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Connected);
            Assert.IsNull(future.Session);
            Assert.IsNull(future.Exception);

            TestThread thread = new TestThread(future);
            thread.Start();

            IoSession session = new DummySession();

            future.SetSession(session);
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Connected);
            Assert.AreSame(session, future.Session);
            Assert.IsNull(future.Exception);

            future = new DefaultConnectFuture();
            thread = new TestThread(future);
            thread.Start();
            future.Exception = new IOException();
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsFalse(future.Connected);
            Assert.IsTrue(future.Exception is IOException);

            try
            {
                IoSession s = future.Session;
                Assert.Fail("IOException should be thrown.");
            }
            catch (Exception)
            {
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void TestWriteFuture()
        {
            DefaultWriteFuture future = new DefaultWriteFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Written);

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Written = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Written);

            future = new DefaultWriteFuture(null);
            thread = new TestThread(future);
            thread.Start();

            future.Exception = new Exception();
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsFalse(future.Written);
            Assert.IsTrue(future.Exception.GetType() == typeof(Exception));
        }

        [TestMethod]
        public void TestAddListener()
        {
            DefaultCloseFuture future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IoFuture f1 = null, f2 = null;

            future.Complete += (s, e) => f1 = e.Future;
            future.Complete += (s, e) => f2 = e.Future;

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(future, f1);
            Assert.AreSame(future, f2);
        }

        [TestMethod]
        public void TestLateAddListener()
        {
            DefaultCloseFuture future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            IoFuture f1 = null;
            future.Complete += (s, e) => f1 = e.Future;
            Assert.AreSame(future, f1);
        }
        
        [TestMethod]
        public void TestRemoveListener1()
        {
            DefaultCloseFuture future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IoFuture f1 = null, f2 = null;
            EventHandler<IoFutureEventArgs> listener1 = (s, e) => f1 = e.Future;
            EventHandler<IoFutureEventArgs> listener2 = (s, e) => f2 = e.Future;

            future.Complete += listener1;
            future.Complete += listener2;
            future.Complete -= listener1;

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(null, f1);
            Assert.AreSame(future, f2);
        }

        [TestMethod]
        public void TestRemoveListener2()
        {
            DefaultCloseFuture future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IoFuture f1 = null, f2 = null;
            EventHandler<IoFutureEventArgs> listener1 = (s, e) => f1 = e.Future;
            EventHandler<IoFutureEventArgs> listener2 = (s, e) => f2 = e.Future;

            future.Complete += listener1;
            future.Complete += listener2;
            future.Complete -= listener2;

            TestThread thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(future, f1);
            Assert.AreSame(null, f2);
        }

        private class TestThread
        {
            public Boolean success;
            readonly IoFuture future;
            readonly Thread t;

            public TestThread(IoFuture future)
            {
                this.future = future;
                this.t = new Thread(Run);
            }

            public void Start()
            {
                t.Start();
            }

            public void Join()
            {
                t.Join();
            }

            public void Run()
            {
                success = future.Await(10000);
            }
        }
    }
}
