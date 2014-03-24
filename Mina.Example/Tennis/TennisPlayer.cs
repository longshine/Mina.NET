using System;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Example.Tennis
{
    class TennisPlayer : IoHandlerAdapter
    {
        private static int nextId = 0;
        /// <summary>
        /// Player ID
        /// </summary>
        private readonly int id = nextId++;

        public override void SessionOpened(IoSession session)
        {
            Console.WriteLine("Player-" + id + ": READY");
        }

        public override void SessionClosed(IoSession session)
        {
            Console.WriteLine("Player-" + id + ": QUIT");
        }

        public override void MessageReceived(IoSession session, Object message)
        {
            Console.WriteLine("Player-" + id + ": RCVD " + message);

            TennisBall ball = (TennisBall)message;

            // Stroke: TTL decreases and PING/PONG state changes.
            ball = ball.Stroke();

            if (ball.TTL > 0)
            {
                // If the ball is still alive, pass it back to peer.
                session.Write(ball);
            }
            else
            {
                // If the ball is dead, this player loses.
                Console.WriteLine("Player-" + id + ": LOSE");
                session.Close(true);
            }
        }

        public override void MessageSent(IoSession session, Object message)
        {
            Console.WriteLine("Player-" + id + ": SENT " + message);
        }

        public override void ExceptionCaught(IoSession session, Exception cause)
        {
            Console.WriteLine(cause);
            session.Close(true);
        }
    }
}
