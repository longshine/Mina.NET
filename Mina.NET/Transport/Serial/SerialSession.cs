#if !UNITY
using System;
using System.IO.Ports;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Serial
{
    class SerialSession : AbstractIoSession, ISerialSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "serial", false, true, typeof(SerialEndPoint));

        private readonly IoProcessor _processor;
        private readonly SerialEndPoint _endpoint;
        private readonly SerialPort _serialPort;
        private readonly IoFilterChain _filterChain;
        private Int32 _writing;

        public SerialSession(SerialConnector service, SerialEndPoint endpoint, SerialPort serialPort)
            : base(service)
        {
            _processor = service;
            base.Config = new SessionConfigImpl(serialPort);
            if (service.SessionConfig != null)
                Config.SetAll(service.SessionConfig);
            _filterChain = new DefaultIoFilterChain(this);
            _serialPort = serialPort;
            _endpoint = endpoint;

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (ReadSuspended || e.EventType == SerialData.Eof)
                return;

            Int32 bytesToRead = _serialPort.BytesToRead;
            Byte[] data = new Byte[bytesToRead];
            Int32 read = _serialPort.Read(data, 0, bytesToRead);
            if (read > 0)
            {
                try
                {
                    FilterChain.FireMessageReceived(IoBuffer.Wrap(data, 0, read));
                }
                catch (Exception ex)
                {
                    this.FilterChain.FireExceptionCaught(ex);
                }
            }
        }

        public override IoProcessor Processor
        {
            get { return _processor; }
        }

        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        public override EndPoint LocalEndPoint
        {
            get { return null; /* not applicable */ }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return _endpoint; }
        }

        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        public new ISerialSessionConfig Config
        {
            get { return (ISerialSessionConfig)base.Config; }
        }

        public SerialPort SerialPort
        {
            get { return _serialPort; }
        }

        public Boolean RtsEnable
        {
            get { return _serialPort.RtsEnable; }
            set { _serialPort.RtsEnable = value; }
        }

        public Boolean DtrEnable
        {
            get { return _serialPort.DtrEnable; }
            set { _serialPort.DtrEnable = value; }
        }

        public void Start()
        {
            _serialPort.Open();
        }

        public void Flush()
        {
            if (WriteSuspended)
                return;
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);
            _serialPort.Dispose();
        }

        private void BeginSend()
        {
            IWriteRequest req = CurrentWriteRequest;
            if (req == null)
            {
                req = WriteRequestQueue.Poll(this);

                if (req == null)
                {
                    Interlocked.Exchange(ref _writing, 0);
                    return;
                }
            }

            IoBuffer buf = req.Message as IoBuffer;

            if (buf == null)
            {
                throw new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?");
            }
            else
            {
                CurrentWriteRequest = req;
                if (buf.HasRemaining)
                    BeginSend(buf);
                else
                    EndSend(0);
            }
        }

        private void BeginSend(IoBuffer buf)
        {
            ArraySegment<Byte> array = buf.GetRemaining();
            try
            {
                _serialPort.BaseStream.BeginWrite(array.Array, array.Offset, array.Count, SendCallback, buf);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                this.FilterChain.FireExceptionCaught(ex);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            IoBuffer buf = (IoBuffer)ar.AsyncState;
            try
            {
                _serialPort.BaseStream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                this.FilterChain.FireExceptionCaught(ex);

                // closed
                Processor.Remove(this);

                return;
            }

            Int32 written = buf.Remaining;
            buf.Position += written;
            EndSend(written);
        }

        private void EndSend(Int32 bytesTransferred)
        {
            this.IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

            IWriteRequest req = CurrentWriteRequest;
            if (req != null)
            {
                IoBuffer buf = req.Message as IoBuffer;
                if (!buf.HasRemaining)
                {
                    // Buffer has been sent, clear the current request.
                    Int32 pos = buf.Position;
                    buf.Reset();

                    CurrentWriteRequest = null;

                    try
                    {
                        this.FilterChain.FireMessageSent(req);
                    }
                    catch (Exception ex)
                    {
                        this.FilterChain.FireExceptionCaught(ex);
                    }

                    // And set it back to its position
                    buf.Position = pos;
                }
            }

            if (_serialPort.IsOpen)
                BeginSend();
        }

        class SessionConfigImpl : IoSessionConfig, ISerialSessionConfig
        {
            private readonly SerialPort _serialPort;
            private Int32 _idleTimeForRead;
            private Int32 _idleTimeForWrite;
            private Int32 _idleTimeForBoth;
            private Int32 _throughputCalculationInterval = 3;

            public SessionConfigImpl(SerialPort serialPort)
            {
                _serialPort = serialPort;
            }

            public void SetAll(IoSessionConfig config)
            {
                if (config == null)
                    throw new ArgumentNullException("config");
                SetIdleTime(IdleStatus.BothIdle, config.GetIdleTime(IdleStatus.BothIdle));
                SetIdleTime(IdleStatus.ReaderIdle, config.GetIdleTime(IdleStatus.ReaderIdle));
                SetIdleTime(IdleStatus.WriterIdle, config.GetIdleTime(IdleStatus.WriterIdle));
                ThroughputCalculationInterval = config.ThroughputCalculationInterval;

                // other properties will be set in SerialConnector.Connect()
            }

            public Int32 ReadTimeout
            {
                get { return _serialPort.ReadTimeout; }
                set { _serialPort.ReadTimeout = value; }
            }

            public Int32 ReadBufferSize
            {
                get { return _serialPort.ReadBufferSize; }
                set { _serialPort.ReadBufferSize = value; }
            }

            public Int32 WriteTimeout
            {
                get { return _serialPort.WriteTimeout; }
                set { _serialPort.WriteTimeout = value; }
            }

            public Int64 WriteTimeoutInMillis
            {
                get { return _serialPort.WriteTimeout * 1000L; }
            }

            public Int32 WriteBufferSize
            {
                get { return _serialPort.WriteBufferSize; }
                set { _serialPort.WriteBufferSize = value; }
            }

            public Int32 ReceivedBytesThreshold
            {
                get { return _serialPort.ReceivedBytesThreshold; }
                set { _serialPort.ReceivedBytesThreshold = value; }
            }

            public Int32 ThroughputCalculationInterval
            {
                get { return _throughputCalculationInterval; }
                set { _throughputCalculationInterval = value; }
            }

            public Int64 ThroughputCalculationIntervalInMillis
            {
                get { return _throughputCalculationInterval * 1000L; }
            }

            public Int32 ReaderIdleTime
            {
                get { return GetIdleTime(IdleStatus.ReaderIdle); }
                set { SetIdleTime(IdleStatus.ReaderIdle, value); }
            }

            public Int32 WriterIdleTime
            {
                get { return GetIdleTime(IdleStatus.WriterIdle); }
                set { SetIdleTime(IdleStatus.WriterIdle, value); }
            }

            public Int32 BothIdleTime
            {
                get { return GetIdleTime(IdleStatus.BothIdle); }
                set { SetIdleTime(IdleStatus.BothIdle, value); }
            }

            public Int32 GetIdleTime(IdleStatus status)
            {
                switch (status)
                {
                    case IdleStatus.ReaderIdle:
                        return _idleTimeForRead;
                    case IdleStatus.WriterIdle:
                        return _idleTimeForWrite;
                    case IdleStatus.BothIdle:
                        return _idleTimeForBoth;
                    default:
                        throw new ArgumentException("Unknown status", "status");
                }
            }

            public Int64 GetIdleTimeInMillis(IdleStatus status)
            {
                return GetIdleTime(status) * 1000L;
            }

            public void SetIdleTime(IdleStatus status, Int32 idleTime)
            {
                switch (status)
                {
                    case IdleStatus.ReaderIdle:
                        _idleTimeForRead = idleTime;
                        break;
                    case IdleStatus.WriterIdle:
                        _idleTimeForWrite = idleTime;
                        break;
                    case IdleStatus.BothIdle:
                        _idleTimeForBoth = idleTime;
                        break;
                    default:
                        throw new ArgumentException("Unknown status", "status");
                }
            }
        }
    }
}
#endif