using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;

namespace EchoServer
{
    class Program
    {
        private static readonly int port = 8080;
        private static readonly Boolean ssl = false;

        static void Main(string[] args)
        {
            IoAcceptor acceptor = new AsyncSocketAcceptor();

            if (ssl)
                acceptor.FilterChain.AddLast("ssl", new SslFilter(AppDomain.CurrentDomain.BaseDirectory + "\\TempCert.cer"));

            acceptor.SessionCreated += s => s.Config.SetIdleTime(IdleStatus.BothIdle, 10);
            acceptor.SessionOpened += s => Console.WriteLine("OPENED");
            acceptor.SessionClosed += s => Console.WriteLine("CLOSED");
            acceptor.SessionIdle += (s, i) => Console.WriteLine("*** IDLE #" + s.GetIdleCount(IdleStatus.BothIdle) + " ***");
            acceptor.ExceptionCaught += (s, e) => s.Close(true);
            acceptor.MessageReceived += (s, m) =>
            {
                Console.WriteLine("Received : " + m);
                IoBuffer income = (IoBuffer)m;
                IoBuffer outcome = ByteBufferAllocator.Instance.Allocate(income.Remaining);
                outcome.Put(income);
                s.Write(outcome);
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

            Console.WriteLine("Listening on port " + port);

            while (true)
            {
                Console.WriteLine("R: " + acceptor.Statistics.ReadBytesThroughput +
                    ", W: " + acceptor.Statistics.WrittenBytesThroughput);
                Thread.Sleep(3000);
            }
        }
    }
}
