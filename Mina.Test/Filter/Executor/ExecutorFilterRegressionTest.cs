using System;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    [TestClass]
    public class ExecutorFilterRegressionTest
    {
        private ExecutorFilter filter = new ExecutorFilter();

        [TestMethod]
        public void TestEventOrder()
        {
            EventOrderChecker nextFilter = new EventOrderChecker();
            EventOrderCounter[] sessions = new EventOrderCounter[] { new EventOrderCounter(),
                new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(),
                new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(),
                new EventOrderCounter(), };
            Int32 loop = 1000000;
            Int32 end = sessions.Length - 1;
            ExecutorFilter filter = this.filter;

            for (Int32 i = 0; i < loop; i++)
            {
                for (Int32 j = end; j >= 0; j--)
                {
                    filter.MessageReceived(nextFilter, sessions[j], i);
                }

                if (nextFilter.exception != null)
                    throw nextFilter.exception;
            }

            System.Threading.Thread.Sleep(2000);

            for (Int32 i = end; i >= 0; i--)
            {
                Assert.AreEqual(loop - 1, sessions[i].LastCount);
            }
        }

        class EventOrderCounter : DummySession
        {
            Int32 _lastCount = -1;

            public Int32 LastCount
            {
                get { return _lastCount; }
                set
                {
                    if (_lastCount > -1)
                        Assert.AreEqual(_lastCount + 1, value);
                    _lastCount = value;
                }
            }
        }

        class EventOrderChecker : INextFilter
        {
            public Exception exception;

            public void MessageReceived(IoSession session, Object message)
            {
                try
                {
                    ((EventOrderCounter)session).LastCount = (Int32)message;
                }
                catch (Exception e)
                {
                    if (exception == null)
                        exception = e;
                }
            }

            public void ExceptionCaught(IoSession session, Exception cause)
            {
                throw new NotImplementedException();
            }

            public void FilterClose(IoSession session)
            {
                // Do nothing
            }

            public void FilterWrite(IoSession session, IWriteRequest writeRequest)
            {
                // Do nothing
            }

            public void MessageSent(IoSession session, IWriteRequest writeRequest)
            {
                // Do nothing
            }

            public void SessionClosed(IoSession session)
            {
                // Do nothing
            }

            public void SessionCreated(IoSession session)
            {
                // Do nothing
            }

            public void SessionIdle(IoSession session, IdleStatus status)
            {
                // Do nothing
            }

            public void SessionOpened(IoSession session)
            {
                // Do nothing
            }

            public void InputClosed(IoSession session)
            {
                // Do nothing
            }
        }
    }
}
