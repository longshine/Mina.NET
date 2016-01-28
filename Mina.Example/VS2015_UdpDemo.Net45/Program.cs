using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Transport.Socket;
using System.Threading;

namespace VS2015_UdpDemo.Net45
{
    class Program
    {
        //client console log prefix string
        static string cp = @"[Client] ";
        //server console log prefix string
        static string sp = @"   [Server] ";

        //Set client and server port
        public const int clientPort = 9527;
        public const int serverPort = 10086;

        //Show for loop iterator
        static int i = 1;
        
        /// <summary>
        /// A demo show how to use Mina.Net as udp client and server
        /// Notice that , console log may not print in order.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            #region Create and config server acceptor
            AsyncDatagramAcceptor acceptor = new AsyncDatagramAcceptor();

            acceptor.SessionConfig.ReuseAddress = true;
            //If idle time greater than 5s , invoke acceptor.SessionIdle
            acceptor.SessionConfig.BothIdleTime = 5;

            InitServerSessionEvent(acceptor);

            acceptor.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            Console.WriteLine(sp + "UDPServer listening on port " + serverPort);
            #endregion

            #region Create and config client connector
            IoConnector connector = new AsyncDatagramConnector();

            connector.DefaultLocalEndPoint = new IPEndPoint(IPAddress.Loopback, clientPort);

            InitClientSessionEvent(connector);

            #endregion

            //Connect to server.
            IConnectFuture connFuture = connector.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
            connFuture.Await();

            //Once client's future is completed, send GC memory size to server cycled.
            connFuture.Complete += (s, e) =>
            {
                IConnectFuture f = (IConnectFuture)e.Future;
                if (f.Connected)
                {
                    IoSession session = f.Session;

                    for (i = 1; i < 6; i++)
                    {
                        Int64 memory = GC.GetTotalMemory(false);
                        IoBuffer buffer = IoBuffer.Allocate(8);
                        buffer.PutInt64(memory);
                        buffer.Flip();
                        session.Write(buffer);

                        try
                        {
                            Thread.Sleep(3000);
                        }
                        catch (ThreadInterruptedException)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(cp + "Not connected...exiting");
                }
            };

            Console.ReadLine();
        }

        private static void InitServerSessionEvent(AsyncDatagramAcceptor acceptor)
        {
            acceptor.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
                e.Session.Close(true);
            };
            acceptor.MessageReceived += (s, e) =>
            {
                IoBuffer buf = e.Message as IoBuffer;
                if (buf != null)
                {
                    Console.WriteLine(sp + i.ToString()+ " Recv form {0}: {1}", e.Session.RemoteEndPoint, buf.GetInt64());
                }
            };
            acceptor.SessionCreated += (s, e) =>
            {
                Console.WriteLine(sp + "Session created...");
            };
            acceptor.SessionOpened += (s, e) =>
            {
                Console.WriteLine(sp + "Session opened...");
            };
            acceptor.SessionClosed += (s, e) =>
            {
                Console.WriteLine(sp + "Session closed...");
            };
            acceptor.SessionIdle += (s, e) =>
            {
                Console.WriteLine(sp + "Session idle...");
            };
        }

        private static void InitClientSessionEvent(IoConnector client)
        {
            client.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(cp + e.Exception.Message);
            };
            client.MessageReceived += (s, e) =>
            {
                Console.WriteLine(cp + "Session recv...");
            };
            client.MessageSent += (s, e) =>
            {
                Console.WriteLine(cp + "Session sent "+i.ToString());
            };
            client.SessionCreated += (s, e) =>
            {
                Console.WriteLine(cp + "Session created...");
            };
            client.SessionOpened += (s, e) =>
            {
                Console.WriteLine(cp + "Session opened...");
            };
            client.SessionClosed += (s, e) =>
            {
                Console.WriteLine(cp + "Session closed...");
            };
            client.SessionIdle += (s, e) =>
            {
                Console.WriteLine(cp + "Session idle...");
            };
        }

    }
}
