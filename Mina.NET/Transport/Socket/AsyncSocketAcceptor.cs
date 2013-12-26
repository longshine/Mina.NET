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

        private System.Net.Sockets.Socket _listenSocket;

        private BufferManager _bufferManager;
        private Pool<SocketAsyncEventArgsBuffer> _readWritePool;

        private System.Collections.Concurrent.ConcurrentQueue<SocketSession> _newSessions = new System.Collections.Concurrent.ConcurrentQueue<SocketSession>();

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

            _listenSocket = new System.Net.Sockets.Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEP);
            _listenSocket.Listen(Backlog);

            StartAccept(null);

            _idleStatusChecker.Start();
        }

        public override void Unbind()
        {
            _idleStatusChecker.Stop();

            _listenSocket.Close();
            _listenSocket = null;
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

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
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
                    if (log.IsErrorEnabled)
                        log.Error("No enough buffer for newly incoming clients");
                    return;
                }

                try
                {
                    SocketSession session = new AsyncSocketSession(this, this, e.AcceptSocket, readBuffer);

                    InitSession(session, null, null);
                    session.Processor.Add(session);
                }
                catch (Exception ex)
                {
                    ExceptionMonitor.Instance.ExceptionCaught(ex);
                }

                // Accept the next connection request
                StartAccept(e);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }
        }
    }
}
