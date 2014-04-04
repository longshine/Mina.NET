using System;
using System.Net;
using Mina.Core.Session;
using Mina.Example.SumUp.Codec;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Serialization;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.SumUp
{
    class Server
    {
        private static readonly int SERVER_PORT = 8080;
        private static readonly String SUM_KEY = "sum";

        // Set this to false to use object serialization instead of custom codec.
        private static readonly Boolean USE_CUSTOM_CODEC = false;

        static void Main(string[] args)
        {
            AsyncSocketAcceptor acceptor = new AsyncSocketAcceptor();

            if (USE_CUSTOM_CODEC)
            {
                acceptor.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new SumUpProtocolCodecFactory(true)));
            }
            else
            {
                acceptor.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new ObjectSerializationCodecFactory()));
            }

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());

            acceptor.SessionOpened += (s, e) =>
            {
                e.Session.Config.SetIdleTime(IdleStatus.BothIdle, 60);
                e.Session.SetAttribute(SUM_KEY, 0);
            };

            acceptor.SessionIdle += (s, e) =>
            {
                e.Session.Close(true);
            };

            acceptor.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
                e.Session.Close(true);
            };

            acceptor.MessageReceived += (s, e) =>
            {
                // client only sends AddMessage. otherwise, we will have to identify
                // its type using instanceof operator.
                AddMessage am = (AddMessage)e.Message;

                // add the value to the current sum.
                Int32 sum = e.Session.GetAttribute<Int32>(SUM_KEY);
                Int32 value = am.Value;
                Int64 expectedSum = (Int64)sum + value;
                if (expectedSum > Int32.MaxValue || expectedSum < Int32.MinValue)
                {
                    // if the sum overflows or underflows, return error message
                    ResultMessage rm = new ResultMessage();
                    rm.Sequence = am.Sequence; // copy sequence
                    rm.OK = false;
                    e.Session.Write(rm);
                }
                else
                {
                    // sum up
                    sum = (int)expectedSum;
                    e.Session.SetAttribute(SUM_KEY, sum);

                    // return the result message
                    ResultMessage rm = new ResultMessage();
                    rm.Sequence = am.Sequence; // copy sequence
                    rm.OK = true;
                    rm.Value = sum;
                    e.Session.Write(rm);
                }
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));

            Console.WriteLine("Listening on port " + SERVER_PORT);
            Console.ReadLine();
        }
    }
}
