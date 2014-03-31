using System;
using System.Collections.Generic;
using System.Linq;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    [TestClass]
    public class CumulativeProtocolDecoderTest
    {
        ProtocolCodecSession session = new ProtocolCodecSession();
        IoBuffer buf;
        IntegerDecoder decoder;

        [TestInitialize]
        public void SetUp()
        {
            buf = IoBuffer.Allocate(16);
            decoder = new IntegerDecoder();
            session.SetTransportMetadata(new DefaultTransportMetadata("mina", "dummy", false, true, typeof(System.Net.IPEndPoint)));
        }

        [TestMethod]
        public void TestCumulation()
        {
            buf.Put((byte)0);
            buf.Flip();

            decoder.Decode(session, buf, session.DecoderOutput);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            Assert.AreEqual(buf.Limit, buf.Position);

            buf.Clear();
            buf.Put((byte)0);
            buf.Put((byte)0);
            buf.Put((byte)1);
            buf.Flip();

            decoder.Decode(session, buf, session.DecoderOutput);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual(1, session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual(buf.Limit, buf.Position);
        }

        [TestMethod]
        public void TestRepeatitiveDecode()
        {
            for (Int32 i = 0; i < 4; i++)
            {
                buf.PutInt32(i);
            }
            buf.Flip();

            decoder.Decode(session, buf, session.DecoderOutput);
            Assert.AreEqual(4, session.DecoderOutputQueue.Count);
            Assert.AreEqual(buf.Limit, buf.Position);

            List<Object> expected = new List<Object>();

            for (Int32 i = 0; i < 4; i++)
            {
                Assert.IsTrue(session.DecoderOutputQueue.Contains(i));
            }
        }

        [TestMethod]
        public void TestWrongImplementationDetection()
        {
            try
            {
                new WrongDecoder().Decode(session, buf, session.DecoderOutput);
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                // OK
            }
        }

        [TestMethod]
        public void TestBufferDerivation()
        {
            decoder = new DuplicatingIntegerDecoder();

            buf.PutInt32(1);

            // Put some extra byte to make the decoder create an internal buffer.
            buf.Put((byte)0);
            buf.Flip();

            decoder.Decode(session, buf, session.DecoderOutput);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual(1, session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual(buf.Limit, buf.Position);

            // Keep appending to the internal buffer.
            // DuplicatingIntegerDecoder will keep duplicating the internal
            // buffer to disable auto-expansion, and CumulativeProtocolDecoder
            // should detect that user derived its internal buffer.
            // Consequently, CumulativeProtocolDecoder will perform 
            // reallocation to avoid putting incoming data into
            // the internal buffer with auto-expansion disabled.
            for (int i = 2; i < 10; i++)
            {
                buf.Clear();
                buf.PutInt32(i);
                // Put some extra byte to make the decoder keep the internal buffer.
                buf.Put((byte)0);
                buf.Flip();
                buf.Position = 1;

                decoder.Decode(session, buf, session.DecoderOutput);
                Assert.AreEqual(1, session.DecoderOutputQueue.Count);
                Assert.AreEqual(i, session.DecoderOutputQueue.Dequeue());
                Assert.AreEqual(buf.Limit, buf.Position);
            }
        }

        class IntegerDecoder : CumulativeProtocolDecoder
        {
            protected override Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
            {
                Assert.IsTrue(input.HasRemaining);

                if (input.Remaining < 4)
                    return false;

                output.Write(input.GetInt32());
                return true;
            }
        }

        class WrongDecoder : CumulativeProtocolDecoder
        {
            protected override Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
            {
                return true;
            }
        }

        class DuplicatingIntegerDecoder : IntegerDecoder
        {
            protected override Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
            {
                input.Duplicate(); // Will disable auto-expansion.
                Assert.IsFalse(input.AutoExpand);
                return base.DoDecode(session, input, output);
            }
        }
    }
}
