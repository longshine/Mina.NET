using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Transport.Loopback;

namespace Mina.Example.Tennis
{
    class Program
    {
        static void Main(string[] args)
        {
            IoAcceptor acceptor = new LoopbackAcceptor();
            LoopbackEndPoint lep = new LoopbackEndPoint(8080);

            // Set up server
            acceptor.Handler = new TennisPlayer();
            acceptor.Bind(lep);

            // Connect to the server.
            LoopbackConnector connector = new LoopbackConnector();
            connector.Handler = new TennisPlayer();
            IConnectFuture future = connector.Connect(lep);
            future.Await();
            IoSession session = future.Session;

            // Send the first ping message
            session.Write(new TennisBall(10));

            // Wait until the match ends.
            session.CloseFuture.Await();

            acceptor.Unbind();
        }
    }
}
