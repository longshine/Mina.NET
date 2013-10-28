using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            // TODO IoBuffer
            Byte[] buf = (Byte[])session.EncoderOutputQueue.Dequeue();
            Assert.AreEqual(5, buf.Length);
            Assert.AreEqual((Byte)'A', buf[0]);
            Assert.AreEqual((Byte)'B', buf[1]);
            Assert.AreEqual((Byte)'C', buf[2]);
            Assert.AreEqual((Byte)'\r', buf[3]);
            Assert.AreEqual((Byte)'\n', buf[4]);
        }
    }
}
