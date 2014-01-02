using System;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using AssertFailedException = NUnit.Framework.AssertionException;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Filter.Logging;

namespace Mina.Filter.Buffer
{
    [TestClass]
    public class BufferedWriteFilterTest
    {
        [TestMethod]
        public void TestNonExpandableBuffer()
        {
            IoBuffer dest = IoBuffer.Allocate(1);
            Assert.AreEqual(false, dest.AutoExpand);
        }

        [TestMethod]
        public void TestBasicBuffering()
        {
            DummySession sess = new DummySession();
            sess.FilterChain.AddFirst("peer", new DummyFilter());
            sess.FilterChain.AddFirst("logger", new LoggingFilter());
            BufferedWriteFilter bFilter = new BufferedWriteFilter(10);
            sess.FilterChain.AddLast("buffer", bFilter);

            IoBuffer data = IoBuffer.Allocate(1);
            for (byte i = 0; i < 20; i++)
            {
                data.Put((byte)(0x30 + i));
                data.Flip();
                sess.Write(data);
                data.Clear();
            }

            // Add one more byte to overflow the final buffer
            data.Put((byte)0);
            data.Flip();
            sess.Write(data);

            // Flush the final byte
            bFilter.Flush(sess);

            sess.Close(true);
        }

        class DummyFilter : IoFilterAdapter
        {
            private Int32 _counter;

            public override void FilterClose(INextFilter nextFilter, IoSession session)
            {
                Assert.AreEqual(3, _counter);
            }

            public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
            {
                _counter++;

                IoBuffer buf = writeRequest.Message as IoBuffer;
                if (buf == null)
                    throw new AssertFailedException("Wrong message type");
                if (_counter == 3)
                {
                    Assert.AreEqual(1, buf.Limit);
                    Assert.AreEqual(0, buf.Get());
                }
                else
                {
                    Assert.AreEqual(10, buf.Limit);
                }
            }
        }
    }
}
