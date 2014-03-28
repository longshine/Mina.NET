using System;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketSession : SocketSession
    {
        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private readonly SocketAsyncEventArgsBuffer _writeBuffer;
        private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor,System.Net.Sockets.Socket socket,
            SocketAsyncEventArgsBuffer readBuffer, SocketAsyncEventArgsBuffer writeBuffer, Boolean reuseBuffer)
            : base(service, processor, socket)
        {
            _readBuffer = readBuffer;
            _readBuffer.SocketAsyncEventArgs.UserToken = this;
            _writeBuffer = writeBuffer;
            _writeBuffer.SocketAsyncEventArgs.UserToken = this;
            _completeHandler = saea_Completed;
            ReuseBuffer = reuseBuffer;
        }

        /// <summary>
        /// Gets the reading buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer ReadBuffer
        {
            get { return _readBuffer; }
        }

        /// <summary>
        /// Gets the writing buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer WriteBuffer
        {
            get { return _writeBuffer; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the internal
        /// <see cref="ReadBuffer"/> as the buffer sent to <see cref="SocketSession.FilterChain"/>
        /// by <see cref="Core.Filterchain.IoFilterChain.FireMessageReceived(Object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IoBuffer"/>
        /// in <see cref="Core.Filterchain.IoFilter.MessageReceived(Core.Filterchain.INextFilter, Core.Session.IoSession, Object)"/>,
        /// this must be set to <code>false</code> since the internal read buffer
        /// will be reset every time a session begins to receive.
        /// </remarks>
        /// <seealso cref="AbstractSocketAcceptor.ReuseBuffer"/>
        public Boolean ReuseBuffer { get; set; }

        /// <inheritdoc/>
        protected override void BeginSend(IoBuffer buf)
        {
            _writeBuffer.Clear();

            SocketAsyncEventArgs saea;
            SocketAsyncEventArgsBuffer saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
                if (_writeBuffer.Remaining < buf.Remaining)
                {
                    Int32 oldLimit = buf.Limit;
                    buf.Limit = buf.Position + _writeBuffer.Remaining;
                    _writeBuffer.Put(buf);
                    buf.Limit = oldLimit;
                }
                else
                {
                    _writeBuffer.Put(buf);
                }
                _writeBuffer.Flip();
                saea = _writeBuffer.SocketAsyncEventArgs;
                saea.SetBuffer(saea.Offset + _writeBuffer.Position, _writeBuffer.Limit);
            }
            else
            {
                saea = saeaBuf.SocketAsyncEventArgs;
                saea.Completed += _completeHandler;
            }

            try
            {
                Boolean willRaiseEvent = Socket.SendAsync(saea);
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(Core.File.IFileRegion file)
        {
            SocketAsyncEventArgs saea = _writeBuffer.SocketAsyncEventArgs;
            saea.SendPacketsElements = new SendPacketsElement[] {
                new SendPacketsElement(file.FullName)
            };

            try
            {
                Boolean willRaiseEvent = Socket.SendPacketsAsync(saea);
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        void saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= _completeHandler;
            ProcessSend(e);
        }

        /// <summary>
        /// Processes send events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndSend(e.BytesTransferred);
                // TODO e.BytesTransferred == 0
            }
            else
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));

                // closed
                Processor.Remove(this);
            }
        }

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            _readBuffer.Clear();
            try
            {
                Boolean willRaiseEvent = Socket.ReceiveAsync(_readBuffer.SocketAsyncEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(_readBuffer.SocketAsyncEventArgs);
                }
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        /// <summary>
        /// Processes receive events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    _readBuffer.Position = e.BytesTransferred;
                    _readBuffer.Flip();

                    if (ReuseBuffer)
                    {
                        EndReceive(_readBuffer);
                    }
                    else
                    {
                        IoBuffer buf = IoBuffer.Allocate(_readBuffer.Remaining);
                        buf.Put(_readBuffer);
                        buf.Flip();
                        EndReceive(buf);
                    }

                    return;
                }
            }
            else
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }

            // closed
            Processor.Remove(this);
        }
    }
}
