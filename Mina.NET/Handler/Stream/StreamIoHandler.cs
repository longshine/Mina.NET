using System;
using System.IO;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Stream
{
    /// <summary>
    /// A <see cref="IoHandler"/> that adapts asynchronous MINA events to stream I/O.
    /// </summary>
    public abstract class StreamIoHandler : IoHandlerAdapter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StreamIoHandler));
        private static readonly AttributeKey KEY_IN = new AttributeKey(typeof(StreamIoHandler), "in");
        private static readonly AttributeKey KEY_OUT = new AttributeKey(typeof(StreamIoHandler), "out");

        private Int32 _readTimeout;
        private Int32 _writeTimeout;

        /// <summary>
        /// Gets or sets read timeout in seconds.
        /// </summary>
        public Int32 ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        /// <summary>
        /// Gets or sets write timeout in seconds.
        /// </summary>
        public Int32 WriteTimeout
        {
            get { return _writeTimeout; }
            set { _writeTimeout = value; }
        }

        /// <inheritdoc/>
        public override void SessionOpened(IoSession session)
        {
            // Set timeouts
            session.Config.WriteTimeout = _writeTimeout;
            session.Config.SetIdleTime(IdleStatus.ReaderIdle, _readTimeout);

            // Create streams
            IoSessionStream input = new IoSessionStream();
            IoSessionStream output = new IoSessionStream(session);
            session.SetAttribute(KEY_IN, input);
            session.SetAttribute(KEY_OUT, output);
            ProcessStreamIo(session, input, output);
        }

        /// <inheritdoc/>
        public override void SessionClosed(IoSession session)
        {
            IoSessionStream input = session.GetAttribute<IoSessionStream>(KEY_IN);
            IoSessionStream output = session.GetAttribute<IoSessionStream>(KEY_OUT);
            try
            {
                input.Close();
            }
            finally
            {
                output.Close();
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(IoSession session, Object message)
        {
            IoSessionStream input = session.GetAttribute<IoSessionStream>(KEY_IN);
            input.Write((IoBuffer)message);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(IoSession session, Exception cause)
        {
            IOException ioe = cause as IOException;
            if (ioe != null)
            {
                IoSessionStream input = session.GetAttribute<IoSessionStream>(KEY_IN);
                if (input != null)
                {
                    input.Exception = ioe;
                    return;
                }
            }

            if (log.IsWarnEnabled)
                log.Warn("Unexpected exception.", cause);
            session.Close(true);
        }

        /// <inheritdoc/>
        public override void SessionIdle(IoSession session, IdleStatus status)
        {
            if (status == IdleStatus.ReaderIdle)
                throw new IOException("Read timeout");
        }

        /// <summary>
        /// Implement this method to execute your stream I/O logic.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        protected abstract void ProcessStreamIo(IoSession session, System.IO.Stream input, System.IO.Stream output);
    }
}
