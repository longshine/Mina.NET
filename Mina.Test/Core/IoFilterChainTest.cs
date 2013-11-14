using System;
using System.Linq;
using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Filter.Util;

namespace Mina.Core
{
    [TestClass]
    public class IoFilterChainTest
    {
        private DummySession dummySession;
        private DummyHandler handler;
        private IoFilterChain chain;
        String testResult;

        [TestInitialize]
        public void SetUp()
        {
            dummySession = new DummySession();
            handler = new DummyHandler(this);
            dummySession.SetHandler(handler);
            chain = dummySession.FilterChain;
            testResult = String.Empty;
        }

        [TestMethod]
        public void TestAdd()
        {
            chain.AddFirst("A", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            chain.AddFirst("C", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            chain.AddBefore("B", "E", new EventOrderTestFilter(this, 'A'));
            chain.AddBefore("C", "F", new EventOrderTestFilter(this, 'A'));
            chain.AddAfter("B", "G", new EventOrderTestFilter(this, 'A'));
            chain.AddAfter("D", "H", new EventOrderTestFilter(this, 'A'));

            String actual = "";

            foreach (IEntry<IoFilter, INextFilter> e in chain.GetAll())
            {
                actual += e.Name;
            }

            Assert.AreEqual("FCAEBGDH", actual);
        }

        [TestMethod]
        public void TestGet()
        {
            IoFilter filterA = new NoopFilter();
            IoFilter filterB = new NoopFilter();
            IoFilter filterC = new NoopFilter();
            IoFilter filterD = new NoopFilter();

            chain.AddFirst("A", filterA);
            chain.AddLast("B", filterB);
            chain.AddBefore("B", "C", filterC);
            chain.AddAfter("A", "D", filterD);

            Assert.AreSame(filterA, chain.Get("A"));
            Assert.AreSame(filterB, chain.Get("B"));
            Assert.AreSame(filterC, chain.Get("C"));
            Assert.AreSame(filterD, chain.Get("D"));
        }

        [TestMethod]
        public void TestRemove()
        {
            chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("C", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("E", new EventOrderTestFilter(this, 'A'));

            chain.Remove("A");
            chain.Remove("E");
            chain.Remove("C");
            chain.Remove("B");
            chain.Remove("D");

            Assert.AreEqual(0, chain.GetAll().Count());
        }

        [TestMethod]
        public void TestClear()
        {
            chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("C", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("E", new EventOrderTestFilter(this, 'A'));

            chain.Clear();

            Assert.AreEqual(0, chain.GetAll().Count());
        }

        [TestMethod]
        public void TestDefault()
        {
            Run("HS0 HSO HMR HMS HSI HEC HSC");
        }

        [TestMethod]
        public void TestChained()
        {
            chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            chain.AddLast("B", new EventOrderTestFilter(this, 'B'));
            Run("AS0 BS0 HS0" + "ASO BSO HSO" + "AMR BMR HMR" + "BFW AFW AMS BMS HMS" + "ASI BSI HSI" + "AEC BEC HEC"
                    + "ASC BSC HSC");
        }

        [TestMethod]
        public void TestAddRemove()
        {
            IoFilter filter = new AddRemoveTestFilter(this);

            chain.AddFirst("A", filter);
            Assert.AreEqual("ADDED", testResult);

            chain.Remove("A");
            Assert.AreEqual("ADDEDREMOVED", testResult);
        }

        private void Run(String expectedResult)
        {
            chain.FireSessionCreated();
            chain.FireSessionOpened();
            chain.FireMessageReceived(new Object());
            chain.FireFilterWrite(new DefaultWriteRequest(new Object()));
            chain.FireSessionIdle(IdleStatus.ReaderIdle);
            chain.FireExceptionCaught(new Exception());
            chain.FireSessionClosed();

            testResult = FormatResult(testResult);
            String formatedExpectedResult = FormatResult(expectedResult);

            Assert.AreEqual(formatedExpectedResult, testResult);
        }

        private String FormatResult(String result)
        {
            String newResult = result.Replace(" ", "");
            StringBuilder buf = new StringBuilder(newResult.Length * 4 / 3);

            for (int i = 0; i < newResult.Length; i++)
            {
                buf.Append(newResult[i]);

                if (i % 3 == 2)
                {
                    buf.Append(' ');
                }
            }

            return buf.ToString();
        }

        class DummyHandler : IoHandlerAdapter
        {
            private readonly IoFilterChainTest test;

            public DummyHandler(IoFilterChainTest test)
            {
                this.test = test;
            }

            public override void SessionCreated(IoSession session)
            {
                test.testResult += "HS0";
            }

            public override void SessionOpened(IoSession session)
            {
                test.testResult += "HSO";
            }

            public override void SessionClosed(IoSession session)
            {
                test.testResult += "HSC";
            }

            public override void SessionIdle(IoSession session, IdleStatus status)
            {
                test.testResult += "HSI";
            }

            public override void ExceptionCaught(IoSession session, Exception cause)
            {
                test.testResult += "HEC";
            }

            public override void MessageReceived(IoSession session, Object message)
            {
                test.testResult += "HMR";
            }

            public override void MessageSent(IoSession session, Object message)
            {
                test.testResult += "HMS";
            }
        }

        class EventOrderTestFilter : IoFilterAdapter
        {
            private readonly char id;
            private readonly IoFilterChainTest test;

            public EventOrderTestFilter(IoFilterChainTest test, char id)
            {
                this.test = test;
                this.id = id;
            }

            public override void SessionCreated(INextFilter nextFilter, IoSession session)
            {
                test.testResult += id + "S0";
                nextFilter.SessionCreated(session);
            }

            public override void SessionOpened(INextFilter nextFilter, IoSession session)
            {
                test.testResult += id + "SO";
                nextFilter.SessionOpened(session);
            }

            public override void SessionClosed(INextFilter nextFilter, IoSession session)
            {
                test.testResult += id + "SC";
                nextFilter.SessionClosed(session);
            }

            public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
            {
                test.testResult += id + "SI";
                nextFilter.SessionIdle(session, status);
            }

            public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
            {
                test.testResult += id + "EC";
                nextFilter.ExceptionCaught(session, cause);
            }

            public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
            {
                test.testResult += id + "FW";
                nextFilter.FilterWrite(session, writeRequest);
            }

            public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
            {
                test.testResult += id + "MR";
                nextFilter.MessageReceived(session, message);
            }

            public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
            {
                test.testResult += id + "MS";
                nextFilter.MessageSent(session, writeRequest);
            }

            public override void FilterClose(INextFilter nextFilter, IoSession session)
            {
                nextFilter.FilterClose(session);
            }
        }

        private class AddRemoveTestFilter : IoFilterAdapter
        {
            private readonly IoFilterChainTest test;

            public AddRemoveTestFilter(IoFilterChainTest test)
            {
                this.test = test;
            }

            public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
            {
                test.testResult += "ADDED";
            }

            public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
            {
                test.testResult += "REMOVED";
            }
        }
    }
}
