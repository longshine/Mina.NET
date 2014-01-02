using System;
using System.Collections.Generic;
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
    public class TextLineDecoderTest
    {
        [TestMethod]
        public void TestNormalDecode()
        {
            Encoding encoding = Encoding.UTF8;
            TextLineDecoder decoder = new TextLineDecoder(encoding, LineDelimiter.Windows);

            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolDecoderOutput output = session.DecoderOutput;
            IoBuffer input = IoBuffer.Allocate(16);

            // Test one decode and one output.put
            input.PutString("ABC\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("ABC", session.DecoderOutputQueue.Dequeue());

            // Test two decode and one output.put
            input.Clear();
            input.PutString("DEF", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("GHI\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("DEFGHI", session.DecoderOutputQueue.Dequeue());

            // Test one decode and two output.put
            input.Clear();
            input.PutString("JKL\r\nMNO\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("JKL", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("MNO", session.DecoderOutputQueue.Dequeue());

            // Test aborted delimiter (DIRMINA-506)
            input.Clear();
            input.PutString("ABC\r\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("ABC\r", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter
            decoder = new TextLineDecoder(encoding, new LineDelimiter("\n\n\n"));
            input.Clear();
            input.PutString("PQR\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter which produces two output.put
            decoder = new TextLineDecoder(encoding, new LineDelimiter("\n\n\n"));
            input.Clear();
            input.PutString("PQR\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\nSTU\n\n\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("STU", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter mixed with partial non-delimiter.
            decoder = new TextLineDecoder(encoding, new LineDelimiter("\n\n\n"));
            input.Clear();
            input.PutString("PQR\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("X\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\n\nSTU\n\n\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR\nX", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("STU", session.DecoderOutputQueue.Dequeue());
        }

        [TestMethod]
        public void TestAutoDecode()
        {
            Encoding encoding = Encoding.UTF8;
            TextLineDecoder decoder = new TextLineDecoder(encoding, LineDelimiter.Auto);

            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolDecoderOutput output = session.DecoderOutput;
            IoBuffer input = IoBuffer.Allocate(16);

            // Test one decode and one output
            input.PutString("ABC\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("ABC", session.DecoderOutputQueue.Dequeue());

            // Test two decode and one output
            input.Clear();
            input.PutString("DEF", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("GHI\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("DEFGHI", session.DecoderOutputQueue.Dequeue());

            // Test one decode and two output
            input.Clear();
            input.PutString("JKL\r\nMNO\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("JKL", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("MNO", session.DecoderOutputQueue.Dequeue());

            // Test multiple '\n's
            input.Clear();
            input.PutString("\n\n\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(3, session.DecoderOutputQueue.Count);
            Assert.AreEqual("", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter (\r\r\n)
            input.Clear();
            input.PutString("PQR\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter (\r\r\n) which produces two output
            input.Clear();
            input.PutString("PQR\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\nSTU\r\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("STU", session.DecoderOutputQueue.Dequeue());

            // Test splitted long delimiter mixed with partial non-delimiter.
            input.Clear();
            input.PutString("PQR\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("X\r", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.Clear();
            input.PutString("\r\nSTU\r\r\n", encoding);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(2, session.DecoderOutputQueue.Count);
            Assert.AreEqual("PQR\rX", session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual("STU", session.DecoderOutputQueue.Dequeue());

            input.Clear();
            String s = encoding.GetString(new byte[] { 0, 77, 105, 110, 97 });
            input.PutString(s, encoding);
            input.Put(0x0a);
            input.Flip();
            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual(s, session.DecoderOutputQueue.Dequeue());
        }

        [TestMethod]
        public void TestOverflow()
        {
            Encoding encoding = Encoding.UTF8;
            TextLineDecoder decoder = new TextLineDecoder(encoding, LineDelimiter.Auto);
            decoder.MaxLineLength = 3;

            ProtocolCodecSession session = new ProtocolCodecSession();
            IProtocolDecoderOutput output = session.DecoderOutput;
            IoBuffer input = IoBuffer.Allocate(16);

            // Make sure the overflow exception is not thrown until
            // the delimiter is encountered.
            input.PutString("A", encoding).Flip().Mark();
            decoder.Decode(session, input.Reset().Mark(), output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            decoder.Decode(session, input.Reset().Mark(), output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            decoder.Decode(session, input.Reset().Mark(), output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            decoder.Decode(session, input.Reset().Mark(), output);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);

            input.Clear().PutString("A\r\nB\r\n", encoding).Flip();

            try
            {
                decoder.Decode(session, input, output);
                Assert.Fail();
            }
            catch (RecoverableProtocolDecoderException)
            {
                // signifies a successful test execution
                Assert.IsTrue(true);
            }

            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("B", session.DecoderOutputQueue.Dequeue());

            //// Make sure OOM is not thrown.
            GC.Collect();
            //long oldFreeMemory = GC.GetTotalMemory(false);
            input = IoBuffer.Allocate(1048576 * 16).Sweep((byte)' ').Mark();

            for (int i = 0; i < 10; i++)
            {
                input.Reset();
                input.Mark();
                decoder.Decode(session, input, output);
                Assert.AreEqual(0, session.DecoderOutputQueue.Count);

                // Memory consumption should be minimal.
                //Assert.IsTrue(GC.GetTotalMemory(false) - oldFreeMemory < 1048576);
            }

            input.Clear().PutString("C\r\nD\r\n", encoding).Flip();
            try
            {
                decoder.Decode(session, input, output);
                Assert.Fail();
            }
            catch (RecoverableProtocolDecoderException)
            {
                // signifies a successful test execution
                Assert.IsTrue(true);
            }

            decoder.Decode(session, input, output);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("D", session.DecoderOutputQueue.Dequeue());

            // Memory consumption should be minimal.
            //Assert.IsTrue(GC.GetTotalMemory(false) - oldFreeMemory < 1048576);
        }

        [TestMethod]
        public void TestSMTPDataBounds()
        {
            Encoding encoding = Encoding.ASCII;
            TextLineDecoder decoder = new TextLineDecoder(encoding, new LineDelimiter("\r\n.\r\n"));

            ProtocolCodecSession session = new ProtocolCodecSession();
            IoBuffer input = IoBuffer.Allocate(16);
            input.AutoExpand = true;

            input.PutString("\r\n", encoding).Flip().Mark();
            decoder.Decode(session, input.Reset().Mark(), session.DecoderOutput);
            Assert.AreEqual(0, session.DecoderOutputQueue.Count);
            input.PutString("Body\r\n.\r\n", encoding);
            decoder.Decode(session, input.Reset().Mark(), session.DecoderOutput);
            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual("\r\n\r\nBody", session.DecoderOutputQueue.Dequeue());
        }
    }
}
