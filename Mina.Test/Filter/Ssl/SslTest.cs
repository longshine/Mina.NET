using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Filterchain;
using Mina.Core.Buffer;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Transport.Socket;

namespace Mina.Filter.Ssl
{
    [TestClass]
    public class SslTest
    {
        private static readonly Int32 port = 5555;

        [TestMethod]
        public void TestSSL()
        {
            StartServer();

            Exception clientError = null;

            Task task = Task.Factory.StartNew(() =>
            {
                try
                {
                    StartClient();
                }
                catch (Exception e)
                {
                    clientError = e;
                }
            });

            task.Wait();

            if (clientError != null)
                throw clientError;
        }

        private static void StartServer()
        {
            AsyncSocketAcceptor acceptor = new AsyncSocketAcceptor();

            DefaultIoFilterChainBuilder filters = acceptor.FilterChain;

            // Inject the SSL filter

            SslFilter sslFilter = new SslFilter(AppDomain.CurrentDomain.BaseDirectory + "\\TempCert.cer");
            filters.AddLast("sslFilter", sslFilter);

            // Inject the TestLine codec filter
            filters.AddLast("text", new ProtocolCodecFilter(new TextLineCodecFactory()));

            acceptor.MessageReceived += (s, e) =>
            {
                String line = (String)e.Message;

                if (line.StartsWith("hello"))
                {
                    Debug.WriteLine("Server got: 'hello', waiting for 'send'");
                    Thread.Sleep(1500);
                }
                else if (line.StartsWith("send"))
                {
                    Debug.WriteLine("Server got: 'send', sending 'data'");
                    e.Session.Write("data");
                }
                else
                {
                    Debug.WriteLine("Server got: " + line);
                }
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        private static void StartClient()
        {
            ConnectAndSend();
            
            ConnectAndSend();

            ClientConnect();
        }

        private static void ClientConnect()
        {
            using (var client = new AsyncSocketConnector())
            using (var ready = new System.Threading.ManualResetEventSlim(false))
            {
                client.FilterChain.AddLast("ssl", new SslFilter("TempCert", null));
                client.FilterChain.AddLast("text", new ProtocolCodecFilter(new TextLineCodecFactory()));

                client.MessageReceived += (s, e) =>
                {
                    Debug.WriteLine("Client got: " + e.Message);
                    ready.Set();
                };

                var session = client.Connect(new IPEndPoint(IPAddress.Loopback, port)).Await().Session;

                Debug.WriteLine("Client sending: hello");
                session.Write("hello                      ");

                Debug.WriteLine("Client sending: send");
                session.Write("send");

                ready.Wait(3000);
                Assert.IsTrue(ready.IsSet);

                ready.Reset();
                Assert.IsFalse(ready.IsSet);

                Debug.WriteLine("Client sending: hello");
                session.Write(IoBuffer.Wrap(Encoding.UTF8.GetBytes("hello                      \n")));

                Debug.WriteLine("Client sending: send");
                session.Write(IoBuffer.Wrap(Encoding.UTF8.GetBytes("send\n")));

                ready.Wait(3000);
                Assert.IsTrue(ready.IsSet);

                session.Close(true);
            }
        }

        private static void ConnectAndSend()
        {
            TcpClient client = new TcpClient("localhost", port);

            SslStream sslStream = new SslStream(
                client.GetStream(),
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
                );
            // The server name must match the name on the server certificate.
            sslStream.AuthenticateAsClient("TempCert");

            Debug.WriteLine("Client sending: hello");
            sslStream.Write(Encoding.UTF8.GetBytes("hello                      \n"));
            sslStream.Flush();

            Debug.WriteLine("Client sending: send");
            sslStream.Write(Encoding.UTF8.GetBytes("send\n"));
            sslStream.Flush();

            String line = ReadMessage(sslStream);
            Debug.WriteLine("Client got: " + line);
            client.Close();
            Assert.IsTrue(!String.IsNullOrEmpty(line));
        }

        static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("\n") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }

        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}
