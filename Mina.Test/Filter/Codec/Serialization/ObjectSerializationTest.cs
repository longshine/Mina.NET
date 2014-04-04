using System;
using System.IO;
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

namespace Mina.Filter.Codec.Serialization
{
    [TestClass]
    public class ObjectSerializationTest
    {
        [TestMethod]
        public void TestEncoder()
        {
            String expected = "1234";

            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolEncoderOutput output = session.EncoderOutput;

            IProtocolEncoder encoder = new ObjectSerializationEncoder();
            encoder.Encode(session, expected, output);

            Assert.AreEqual(1, session.EncoderOutputQueue.Count);
            IoBuffer buf = (IoBuffer)session.EncoderOutputQueue.Dequeue();

            TestDecoderAndInputStream(expected, buf);
        }

        private void TestDecoderAndInputStream(String expected, IoBuffer input)
        {
            // Test ProtocolDecoder
            IProtocolDecoder decoder = new ObjectSerializationDecoder();
            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolDecoderOutput decoderOut = session.DecoderOutput;
            decoder.Decode(session, input.Duplicate(), decoderOut);

            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual(expected, session.DecoderOutputQueue.Dequeue());
        }
    }
}
