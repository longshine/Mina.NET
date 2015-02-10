using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Transport.Socket;

namespace MulticastUDP
{
    /// <summary>
    /// UDP Multicast
    /// 
    /// See http://msdn.microsoft.com/en-us/library/system.net.sockets.multicastoption%28v=vs.110%29.aspx
    /// </summary>
    class Program
    {
        static IPAddress mcastAddress;
        static int mcastPort;

        static void Main(string[] args)
        {
            // Initialize the multicast address group and multicast port. 
            // Both address and port are selected from the allowed sets as 
            // defined in the related RFC documents. These are the same  
            // as the values used by the sender.
            mcastAddress = IPAddress.Parse("224.168.100.2");
            mcastPort = 11000;

            StartMulticastAcceptor();
            StartMulticastConnector();

            Console.ReadLine();
        }

        static void StartMulticastAcceptor()
        {
            IPAddress localIPAddr = IPAddress.Any;
            AsyncDatagramAcceptor acceptor = new AsyncDatagramAcceptor();

            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            // Define a MulticastOption object specifying the multicast group  
            // address and the local IPAddress. 
            // The multicast group address is the same as the address used by the client.
            MulticastOption mcastOption = new MulticastOption(mcastAddress, localIPAddr);
            acceptor.SessionConfig.MulticastOption = mcastOption;

            acceptor.SessionOpened += (s, e) =>
            {
                Console.WriteLine("Opened: {0}", e.Session.RemoteEndPoint);
            };
            acceptor.MessageReceived += (s, e) =>
            {
                Console.WriteLine("Received from {0}: {1}", e.Session.RemoteEndPoint, e.Message);
            };

            acceptor.Bind(new IPEndPoint(localIPAddr, mcastPort));

            Console.WriteLine("Acceptor: current multicast group is: " + mcastOption.Group);
            Console.WriteLine("Acceptor: current multicast local address is: " + mcastOption.LocalAddress);
            Console.WriteLine("Waiting for multicast packets.......");
        }

        static void StartMulticastConnector()
        {
            IPAddress localIPAddr = IPAddress.Any;
            IPEndPoint mcastEP = new IPEndPoint(mcastAddress, mcastPort);
            AsyncDatagramConnector connector = new AsyncDatagramConnector();

            connector.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            // Set the local IP address used by the listener and the sender to 
            // exchange multicast messages. 
            connector.DefaultLocalEndPoint = new IPEndPoint(localIPAddr, 0);

            // Define a MulticastOption object specifying the multicast group  
            // address and the local IP address. 
            // The multicast group address is the same as the address used by the listener.
            MulticastOption mcastOption = new MulticastOption(mcastAddress, localIPAddr);
            connector.SessionConfig.MulticastOption = mcastOption;

            // Call Connect() to force binding to the local IP address,
            // and get the associated multicast session.
            IoSession session = connector.Connect(mcastEP).Await().Session;

            // Send multicast packets to the multicast endpoint.
            session.Write("hello 1", mcastEP);
            session.Write("hello 2", mcastEP);
            session.Write("hello 3", mcastEP);
        }
    }
}
