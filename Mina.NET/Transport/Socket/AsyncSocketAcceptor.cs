using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Common.Logging;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketAcceptor : AbstractSocketAcceptor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncSocketAcceptor));

        private BufferManager _bufferManager;
        private Pool<SocketAsyncEventArgsBuffer> _readWritePool;

        public AsyncSocketAcceptor()
            : this(1024)
        { }

        public AsyncSocketAcceptor(Int32 maxConnections)
            : base(maxConnections)
        {
            this.SessionDestroyed += OnSessionDestroyed;
        }

        public override void Bind(EndPoint localEP)
        {
            InitBuffer();
            base.Bind(localEP);
        }

        private void InitBuffer()
        {
            Int32 bufferSize = SessionConfig.ReadBufferSize;
            if (_bufferManager == null || _bufferManager.BufferSize != bufferSize)
            {
                // TODO free previous pool

                _bufferManager = new BufferManager(bufferSize * MaxConnections, bufferSize);
                _bufferManager.InitBuffer();

                var list = new List<SocketAsyncEventArgsBuffer>(MaxConnections);
                for (Int32 i = 0; i < MaxConnections; i++)
                {
                    SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                    _bufferManager.SetBuffer(readWriteEventArg);
                    SocketAsyncEventArgsBuffer buf = new SocketAsyncEventArgsBuffer(readWriteEventArg);
                    list.Add(buf);

                    readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(readWriteEventArg_Completed);
                }
                _readWritePool = new Pool<SocketAsyncEventArgsBuffer>(list);
            }
        }

        void readWriteEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            AsyncSocketSession session = e.UserToken as AsyncSocketSession;

            if (session == null)
                return;

            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                session.ProcessReceive(e);
            }
            else
            {
                // TODO handle other Socket operations
            }
        }

        private void OnSessionDestroyed(IoSession session)
        {
            AsyncSocketSession s = session as AsyncSocketSession;
            if (s != null && _readWritePool != null)
                _readWritePool.Push(s.ReadBuffer);
        }

        protected override void BeginAccept(Object state)
        {
            SocketAsyncEventArgs acceptEventArg = (SocketAsyncEventArgs)state;
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(Object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgsBuffer readBuffer = _readWritePool.Pop();

                if (readBuffer == null)
                {
                    readBuffer = (SocketAsyncEventArgsBuffer)
                        SocketAsyncEventArgsBufferAllocator.Instance.Allocate(SessionConfig.ReadBufferSize);
                    readBuffer.SocketAsyncEventArgs.Completed += readWriteEventArg_Completed;
                }

                EndAccept(new AsyncSocketSession(this, this, e.AcceptSocket, readBuffer), e);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }
        }
    }
}
