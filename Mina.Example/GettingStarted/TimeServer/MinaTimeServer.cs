using System;
using System.Net;
using System.Text;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.GettingStarted.TimeServer
{
    /// <summary>
    /// A minimal 'time' server, returning the current date. Opening
    /// a telnet server, you will get the current date by typing
    /// any string followed by a new line.
    /// 
    /// In order to quit, just send the 'quit' message.
    /// </summary>
    class MinaTimeServer
    {
        private static readonly Int32 port = 9123;

        /// <summary>
        /// The server implementation. It's based on TCP, and uses a logging filter 
        /// plus a text line decoder.
        /// </summary>
        static void Main(string[] args)
        {
            // Create the acceptor
            IoAcceptor acceptor = new AsyncSocketAcceptor();

            // Add two filters : a logger and a codec
            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            // Attach the business logic to the server
            acceptor.Handler = new TimeServerHandler();

            // Configurate the buffer size and the iddle time
            acceptor.SessionConfig.ReadBufferSize = 2048;
            acceptor.SessionConfig.SetIdleTime(IdleStatus.BothIdle, 10);

            // And bind !
            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

            Console.ReadLine();
        }
    }
}
