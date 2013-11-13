using System;
using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.TextLine
{
    [TestClass]
    public class TextLineEncoderTest
    {
        [TestMethod]
        public void TestEncode()
        {
            TextLineEncoder encoder = new TextLineEncoder(Encoding.UTF8, LineDelimiter.Windows);
            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolEncoderOutput output = session.EncoderOutput;

            encoder.Encode(session, "ABC", output);
            Assert.AreEqual(1, session.EncoderOutputQueue.Count);
            IoBuffer buf = (IoBuffer)session.EncoderOutputQueue.Dequeue();
            Assert.AreEqual(5, buf.Remaining);
            Assert.AreEqual((Byte)'A', buf.Get());
            Assert.AreEqual((Byte)'B', buf.Get());
            Assert.AreEqual((Byte)'C', buf.Get());
            Assert.AreEqual((Byte)'\r', buf.Get());
            Assert.AreEqual((Byte)'\n', buf.Get());
        }
    }
}
