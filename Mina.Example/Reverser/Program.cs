using System;
using System.Net;
using System.Text;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.Reverser
{
    /// <summary>
    /// Reverser server which reverses all text lines from
    /// </summary>
    class Program
    {
        private const int port = 8080;

        static void Main(string[] args)
        {
            IoAcceptor acceptor = new AsyncSocketAcceptor();

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(
                new TextLineCodecFactory(Encoding.UTF8)));

            acceptor.ExceptionCaught += (s, e) => e.Session.Close(true);
            acceptor.MessageReceived += (s, e) =>
            {
                String str = e.Message.ToString();
                StringBuilder sb = new StringBuilder(str.Length);
                for (int i = str.Length - 1; i >= 0; i--)
                {
                    sb.Append(str[i]);
                }
                e.Session.Write(sb.ToString());
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

            Console.WriteLine("Listening on " + acceptor.LocalEndPoint);
            Console.ReadLine();
        }
    }
}
