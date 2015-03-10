#if !UNITY
using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// <see cref="IoConnector"/> for serial communication transport.
    /// </summary>
    public class SerialConnector : AbstractIoConnector, IoProcessor<SerialSession>
    {
        private readonly IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public SerialConnector()
            : base(new DefaultSerialSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public new ISerialSessionConfig SessionConfig
        {
            get { return (ISerialSessionConfig)base.SessionConfig; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return SerialSession.Metadata; }
        }

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            ISerialSessionConfig config = (ISerialSessionConfig)SessionConfig;
            SerialEndPoint sep = (SerialEndPoint)remoteEP;

            SerialPort serialPort = new SerialPort(sep.PortName, sep.BaudRate, sep.Parity, sep.DataBits, sep.StopBits);
            if (config.ReadBufferSize > 0)
                serialPort.ReadBufferSize = config.ReadBufferSize;
            if (config.ReadTimeout > 0)
                serialPort.ReadTimeout = config.ReadTimeout * 1000;
            if (config.WriteBufferSize > 0)
                serialPort.WriteBufferSize = config.WriteBufferSize;
            if (config.WriteTimeout > 0)
                serialPort.WriteTimeout = config.WriteTimeout * 1000;
            if (config.ReceivedBytesThreshold > 0)
                serialPort.ReceivedBytesThreshold = config.ReceivedBytesThreshold;

            IConnectFuture future = new DefaultConnectFuture();
            SerialSession session = new SerialSession(this, sep, serialPort);
            InitSession(session, future, sessionInitializer);

            try
            {
                session.Processor.Add(session);
            }
            catch (IOException ex)
            {
                return DefaultConnectFuture.NewFailedFuture(ex);
            }

            _idleStatusChecker.Start();

            return future;
        }

        /// <summary>
        /// Disposes.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _idleStatusChecker.Dispose();
            }
            base.Dispose(disposing);
        }

        #region IoProcessor

        private void Add(SerialSession session)
        {
            // Build the filter chain of this session.
            session.Service.FilterChainBuilder.BuildFilterChain(session.FilterChain);

            // Propagate the SESSION_CREATED event up to the chain
            IoServiceSupport serviceSupport = session.Service as IoServiceSupport;
            if (serviceSupport != null)
                serviceSupport.FireSessionCreated(session);

            session.Start();
        }

        private void Write(SerialSession session, IWriteRequest writeRequest)
        {
            IWriteRequestQueue writeRequestQueue = session.WriteRequestQueue;
            writeRequestQueue.Offer(session, writeRequest);
            if (!session.WriteSuspended)
                Flush(session);
        }

        private void Flush(SerialSession session)
        {
            session.Flush();
        }

        private void Remove(SerialSession session)
        {
            session.SerialPort.Close();
            IoServiceSupport support = session.Service as IoServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        private void UpdateTrafficControl(SerialSession session)
        {
            if (!session.WriteSuspended)
                Flush(session);
        }

        void IoProcessor<SerialSession>.Add(SerialSession session)
        {
            Add(session);
        }

        void IoProcessor<SerialSession>.Write(SerialSession session, IWriteRequest writeRequest)
        {
            Write(session, writeRequest);
        }

        void IoProcessor<SerialSession>.Flush(SerialSession session)
        {
            Flush(session);
        }

        void IoProcessor<SerialSession>.Remove(SerialSession session)
        {
            Remove(session);
        }

        void IoProcessor<SerialSession>.UpdateTrafficControl(SerialSession session)
        {
            UpdateTrafficControl(session);
        }

        void IoProcessor.Add(IoSession session)
        {
            Add((SerialSession)session);
        }

        void IoProcessor.Write(IoSession session, IWriteRequest writeRequest)
        {
            Write((SerialSession)session, writeRequest);
        }

        void IoProcessor.Flush(IoSession session)
        {
            Flush((SerialSession)session);
        }

        void IoProcessor.Remove(IoSession session)
        {
            Remove((SerialSession)session);
        }

        void IoProcessor.UpdateTrafficControl(IoSession session)
        {
            UpdateTrafficControl((SerialSession)session);
        }

        #endregion
    }
}
#endif