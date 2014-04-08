using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
using Mina.Transport.Socket;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractFileTest
    {
        private const Int32 FILE_SIZE = 1 * 1024 * 1024; // 1MB file
        private FileInfo file;

        [TestInitialize]
        public void SetUp()
        {
            file = CreateLargeFile();
        }

        [TestCleanup]
        public void TearDown()
        {
            file.Delete();
        }

        [TestMethod]
        public void TestSendLargeFile()
        {
            Assert.AreEqual(FILE_SIZE, file.Length, "Test file not as big as specified");

            CountdownEvent countdown = new CountdownEvent(1);
            Boolean[] success = { false };
            Exception[] exception = { null };

            Int32 port = 12345;
            IoAcceptor acceptor = CreateAcceptor();
            IoConnector connector = CreateConnector();

            try
            {
                acceptor.ExceptionCaught += (s, e) =>
                {
                    exception[0] = e.Exception;
                    e.Session.Close(true);
                };

                Int32 index = 0;
                acceptor.MessageReceived += (s, e) =>
                {
                    IoBuffer buffer = (IoBuffer)e.Message;
                    while (buffer.HasRemaining)
                    {
                        int x = buffer.GetInt32();
                        if (x != index)
                        {
                            throw new Exception(String.Format("Integer at {0} was {1} but should have been {0}", index, x));
                        }
                        index++;
                    }
                    if (index > FILE_SIZE / 4)
                    {
                        throw new Exception("Read too much data");
                    }
                    if (index == FILE_SIZE / 4)
                    {
                        success[0] = true;
                        e.Session.Close(true);
                    }
                };

                acceptor.Bind(CreateEndPoint(port));

                connector.ExceptionCaught += (s, e) =>
                {
                    exception[0] = e.Exception;
                    e.Session.Close(true);
                };
                connector.SessionClosed += (s, e) => countdown.Signal();

                IConnectFuture future = connector.Connect(CreateEndPoint(port));
                future.Await();

                IoSession session = future.Session;
                session.Write(file);

                countdown.Wait();

                if (exception[0] != null)
                    throw exception[0];

                Assert.IsTrue(success[0], "Did not complete file transfer successfully");
                Assert.AreEqual(1, session.WrittenMessages, "Written messages should be 1 (we wrote one file)");
                Assert.AreEqual(FILE_SIZE, session.WrittenBytes, "Written bytes should match file size");
            }
            finally
            {
                try
                {
                    connector.Dispose();
                }
                finally
                {
                    acceptor.Dispose();
                }
            }
        }

        protected abstract IoAcceptor CreateAcceptor();
        protected abstract IoConnector CreateConnector();
        protected abstract EndPoint CreateEndPoint(Int32 port);

        private static FileInfo CreateLargeFile()
        {
            IoBuffer buffer = IoBuffer.Allocate(FILE_SIZE);
            for (Int32 i = 0; i < FILE_SIZE / 4; i++)
            {
                buffer.PutInt32(i);
            }
            buffer.Flip();

            String path = Path.GetTempFileName();
            Byte[] data = new Byte[buffer.Remaining];
            buffer.Get(data, 0, data.Length);
            File.WriteAllBytes(path, data);
            return new FileInfo(path);
        }
    }
}
