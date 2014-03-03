using System;
using System.Net;
using System.Text;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Transport.Socket;
using Mina.Filter.Executor;

namespace Mina.Example.Haiku
{
    class HaikuValidationServer
    {
        private const int port = 42458;

        static void Main(string[] args)
        {
            /*
             * ReuseBuffer needs to be false since we have a ExecutorFilter before
             * ProtocolCodecFilter which processes incoming IoBuffer.
             */
            IoAcceptor acceptor = new AsyncSocketAcceptor() { ReuseBuffer = false };

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.FilterChain.AddLast("executor", new ExecutorFilter());
            acceptor.FilterChain.AddLast("to-string", new ProtocolCodecFilter(
                new TextLineCodecFactory(Encoding.UTF8)));
            acceptor.FilterChain.AddLast("to-haiki", new ToHaikuIoFilter());

            acceptor.ExceptionCaught += (s, e) => e.Session.Close(true);

            HaikuValidator validator = new HaikuValidator();
            acceptor.MessageReceived += (s, e) =>
            {
                Haiku haiku = (Haiku)e.Message;

                try
                {
                    validator.Validate(haiku);
                    e.Session.Write("HAIKU!");
                }
                catch (InvalidHaikuException ex)
                {
                    e.Session.Write("NOT A HAIKU: " + ex.Message);
                }
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

            Console.WriteLine("Listening on " + acceptor.LocalEndPoint);
            Console.ReadLine();
        }
    }
}
