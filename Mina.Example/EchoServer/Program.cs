using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;

namespace Mina.Example.EchoServer
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

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());

            acceptor.Activated += (s, e) => Console.WriteLine("ACTIVATED");
            acceptor.Deactivated += (s, e) => Console.WriteLine("DEACTIVATED");
            acceptor.SessionCreated += (s, e) => e.Session.Config.SetIdleTime(IdleStatus.BothIdle, 10);
            acceptor.SessionOpened += (s, e) => Console.WriteLine("OPENED");
            acceptor.SessionClosed += (s, e) => Console.WriteLine("CLOSED");
            acceptor.SessionIdle += (s, e) => Console.WriteLine("*** IDLE #" + e.Session.GetIdleCount(IdleStatus.BothIdle) + " ***");
            acceptor.ExceptionCaught += (s, e) => e.Session.Close(true);
            acceptor.MessageReceived += (s, e) =>
            {
                Console.WriteLine("Received : " + e.Message);
                IoBuffer income = (IoBuffer)e.Message;
                IoBuffer outcome = IoBuffer.Allocate(income.Remaining);
                outcome.Put(income);
                outcome.Flip();
                e.Session.Write(outcome);
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

            Console.WriteLine("Listening on " + acceptor.LocalEndPoint);

            while (true)
            {
                Console.WriteLine("R: " + acceptor.Statistics.ReadBytesThroughput +
                    ", W: " + acceptor.Statistics.WrittenBytesThroughput);
                Thread.Sleep(3000);
            }
        }
    }
}
