using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// Base implementation of <see cref="IoSession"/>.
    /// </summary>
    public abstract class AbstractIoSession : IoSession, IDisposable
    {
        private static readonly IWriteRequest CLOSE_REQUEST = new DefaultWriteRequest(new Object());
        private static Int64 idGenerator = 0;
        private Int64 _id;
        private Object _syncRoot = new Byte[0];
        private IoSessionConfig _config;
        private readonly IoService _service;
        private readonly IoHandler _handler;
        private IoSessionAttributeMap _attributes;
        private IWriteRequestQueue _writeRequestQueue;
        private IWriteRequest _currentWriteRequest;
        private readonly DateTime _creationTime;
        private volatile Boolean _closing;
        private readonly ICloseFuture _closeFuture;

        /// <summary>
        /// </summary>
        protected AbstractIoSession(IoService service)
        {
            _service = service;
            _handler = service.Handler;

            _creationTime = DateTime.Now;
            _lastThroughputCalculationTime = _creationTime;
            _lastReadTime = _lastWriteTime = _creationTime;

            _id = Interlocked.Increment(ref idGenerator);

            _closeFuture = new DefaultCloseFuture(this);
            _closeFuture.Complete += ResetCounter;
        }

        /// <inheritdoc/>
        public Int64 Id
        {
            get { return _id; }
        }

        /// <inheritdoc/>
        public IoSessionConfig Config
        {
            get { return _config; }
            protected set { _config = value; }
        }

        /// <inheritdoc/>
        public IoService Service
        {
            get { return _service; }
        }

        /// <inheritdoc/>
        public abstract IoProcessor Processor { get; }

        /// <inheritdoc/>
        public virtual IoHandler Handler
        {
            get { return _handler; }
        }

        /// <inheritdoc/>
        public abstract IoFilterChain FilterChain { get; }

        /// <inheritdoc/>
        public abstract ITransportMetadata TransportMetadata { get; }

        /// <inheritdoc/>
        public Boolean Connected
        {
            get { return !_closeFuture.Closed; }
        }

        /// <inheritdoc/>
        public virtual bool Active
        {
            get
            {
                // Return true by default
                return true;
            }
        }

        /// <inheritdoc/>
        public Boolean Closing
        {
            get { return _closing || _closeFuture.Closed; }
        }

        /// <inheritdoc/>
        public virtual Boolean Secured
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public ICloseFuture CloseFuture
        {
            get { return _closeFuture; }
        }

        /// <inheritdoc/>
        public abstract EndPoint LocalEndPoint { get; }

        /// <inheritdoc/>
        public abstract EndPoint RemoteEndPoint { get; }

        /// <inheritdoc/>
        public IoSessionAttributeMap AttributeMap
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        /// <inheritdoc/>
        public IWriteRequestQueue WriteRequestQueue
        {
            get
            {
                if (_writeRequestQueue == null)
                    throw new InvalidOperationException();
                return _writeRequestQueue;
            }
        }

        /// <inheritdoc/>
        public IWriteRequest CurrentWriteRequest
        {
            get { return _currentWriteRequest; }
            set { _currentWriteRequest = value; }
        }

        /// <inheritdoc/>
        public void SetWriteRequestQueue(IWriteRequestQueue queue)
        {
            _writeRequestQueue = new CloseAwareWriteQueue(this, queue);
        }

        /// <inheritdoc/>
        public IWriteFuture Write(Object message)
        {
            return Write(message, null);
        }

        /// <inheritdoc/>
        public IWriteFuture Write(Object message, EndPoint remoteEP)
        {
            if (message == null)
                return null;

            if (!TransportMetadata.Connectionless && remoteEP != null)
                throw new InvalidOperationException();

            // If the session has been closed or is closing, we can't either
            // send a message to the remote side. We generate a future
            // containing an exception.
            if (Closing || !Connected)
            {
                IWriteFuture future = new DefaultWriteFuture(this);
                IWriteRequest request = new DefaultWriteRequest(message, future, remoteEP);
                future.Exception = new WriteToClosedSessionException(request);
                return future;
            }

            IoBuffer buf = message as IoBuffer;
            if (buf == null)
            {
                System.IO.FileInfo fi = message as System.IO.FileInfo;
                if (fi != null)
                    message = new FileInfoFileRegion(fi);
            }
            else if (!buf.HasRemaining)
            {
                return DefaultWriteFuture.NewNotWrittenFuture(this,
                    new ArgumentException("message is empty. Forgot to call flip()?", "message"));
            }

            // Now, we can write the message. First, create a future
            IWriteFuture writeFuture = new DefaultWriteFuture(this);
            IWriteRequest writeRequest = new DefaultWriteRequest(message, writeFuture, remoteEP);

            // Then, get the chain and inject the WriteRequest into it
            IoFilterChain filterChain = this.FilterChain;
            filterChain.FireFilterWrite(writeRequest);

            return writeFuture;
        }

        /// <inheritdoc/>
        public ICloseFuture Close(Boolean rightNow)
        {
            if (Closing)
            {
                return _closeFuture;
            }
            else if (rightNow)
            {
                return CloseNow();
            }
            else
            {
                return CloseOnFlush();
            }
        }

        /// <summary>
        /// Closes this session immediately. This operation is asynchronous.
        /// </summary>
        [Obsolete("Use Close(bool) instead")]
        public ICloseFuture Close()
        {
            return CloseNow();
        }

        /// <inheritdoc/>
        public ICloseFuture CloseNow()
        {
            lock (_syncRoot)
            {
                if (Closing)
                    return _closeFuture;

                _closing = true;

                try
                {
                    Destroy();
                }
                catch (Exception e)
                {
                    FilterChain.FireExceptionCaught(e);
                }
            }
            FilterChain.FireFilterClose();
            return _closeFuture;
        }

        /// <inheritdoc/>
        public ICloseFuture CloseOnFlush()
        {
            if (!Closing)
            {
                WriteRequestQueue.Offer(this, CLOSE_REQUEST);
                Processor.Flush(this);
            }
            return _closeFuture;
        }

        /// <summary>
        /// Destroy the session.
        /// </summary>
        protected void Destroy()
        {
            if (_writeRequestQueue != null)
            {
                while (!_writeRequestQueue.IsEmpty(this))
                {
                    IWriteRequest writeRequest = _writeRequestQueue.Poll(this);

                    if (writeRequest != null)
                    {
                        IWriteFuture writeFuture = writeRequest.Future;

                        // The WriteRequest may not always have a future:
                        // the CLOSE_REQUEST and MESSAGE_SENT_REQUEST don't.
                        if (writeFuture != null)
                        {
                            writeFuture.Written = true;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public Object GetAttribute(Object key)
        {
            return GetAttribute(key, null);
        }

        /// <inheritdoc/>
        public Object GetAttribute(Object key, Object defaultValue)
        {
            return _attributes.GetAttribute(this, key, defaultValue);
        }

        /// <inheritdoc/>
        public T GetAttribute<T>(Object key)
        {
            return GetAttribute<T>(key, default(T));
        }

        /// <inheritdoc/>
        public T GetAttribute<T>(Object key, T defaultValue)
        {
            return (T)_attributes.GetAttribute(this, key, defaultValue);
        }

        /// <inheritdoc/>
        public Object SetAttribute(Object key, Object value)
        {
            return _attributes.SetAttribute(this, key, value);
        }

        /// <inheritdoc/>
        public Object SetAttribute(Object key)
        {
            return SetAttribute(key, true);
        }

        /// <inheritdoc/>
        public Object SetAttributeIfAbsent(Object key, Object value)
        {
            return _attributes.SetAttributeIfAbsent(this, key, value);
        }

        /// <inheritdoc/>
        public Object SetAttributeIfAbsent(Object key)
        {
            return SetAttributeIfAbsent(key, true);
        }

        /// <inheritdoc/>
        public Object RemoveAttribute(Object key)
        {
            return _attributes.RemoveAttribute(this, key);
        }

        /// <inheritdoc/>
        public Boolean ContainsAttribute(Object key)
        {
            return _attributes.ContainsAttribute(this, key);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                ((DefaultCloseFuture)_closeFuture).Dispose();
            }
        }

        #region Traffic control

        private Boolean _readSuspended = false;
        private Boolean _writeSuspended = false;

        /// <inheritdoc/>
        public Boolean ReadSuspended
        {
            get { return _readSuspended; }
        }

        /// <inheritdoc/>
        public Boolean WriteSuspended
        {
            get { return _writeSuspended; }
        }

        /// <inheritdoc/>
        public void SuspendRead()
        {
            _readSuspended = true;
            if (Closing || !Connected)
                return;
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void SuspendWrite()
        {
            _writeSuspended = true;
            if (Closing || !Connected)
                return;
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void ResumeRead()
        {
            _readSuspended = false;
            if (Closing || !Connected)
                return;
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void ResumeWrite()
        {
            _writeSuspended = false;
            if (Closing || !Connected)
                return;
            Processor.UpdateTrafficControl(this);
        }

        #endregion

        #region Status variables

        private Int32 _scheduledWriteBytes;
        private Int32 _scheduledWriteMessages;
        private Int64 _readBytes;
        private Int64 _writtenBytes;
        private Int64 _readMessages;
        private Int64 _writtenMessages;
        private DateTime _lastReadTime;
        private DateTime _lastWriteTime;
        private DateTime _lastThroughputCalculationTime;
        private Int64 _lastReadBytes;
        private Int64 _lastWrittenBytes;
        private Int64 _lastReadMessages;
        private Int64 _lastWrittenMessages;
        private Double _readBytesThroughput;
        private Double _writtenBytesThroughput;
        private Double _readMessagesThroughput;
        private Double _writtenMessagesThroughput;
        private Int32 _idleCountForBoth;
        private Int32 _idleCountForRead;
        private Int32 _idleCountForWrite;
        private DateTime _lastIdleTimeForBoth;
        private DateTime _lastIdleTimeForRead;
        private DateTime _lastIdleTimeForWrite;

        /// <inheritdoc/>
        public Int64 ReadBytes
        {
            get { return _readBytes; }
        }

        /// <inheritdoc/>
        public Int64 WrittenBytes
        {
            get { return _writtenBytes; }
        }

        /// <inheritdoc/>
        public Int64 ReadMessages
        {
            get { return _readMessages; }
        }

        /// <inheritdoc/>
        public Int64 WrittenMessages
        {
            get { return _writtenMessages; }
        }

        /// <inheritdoc/>
        public Double ReadBytesThroughput
        {
            get { return _readBytesThroughput; }
        }

        /// <inheritdoc/>
        public Double WrittenBytesThroughput
        {
            get { return _writtenBytesThroughput; }
        }

        /// <inheritdoc/>
        public Double ReadMessagesThroughput
        {
            get { return _readMessagesThroughput; }
        }

        /// <inheritdoc/>
        public Double WrittenMessagesThroughput
        {
            get { return _writtenMessagesThroughput; }
        }

        /// <inheritdoc/>
        public DateTime CreationTime
        {
            get { return _creationTime; }
        }

        /// <inheritdoc/>
        public DateTime LastIoTime
        {
            get { return _lastReadTime > _lastWriteTime ? _lastReadTime : _lastWriteTime; }
        }

        /// <inheritdoc/>
        public DateTime LastReadTime
        {
            get { return _lastReadTime; }
        }

        /// <inheritdoc/>
        public DateTime LastWriteTime
        {
            get { return _lastWriteTime; }
        }

        /// <inheritdoc/>
        public Boolean IsIdle(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth > 0;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead > 0;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite > 0;
                default:
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <inheritdoc/>
        public Boolean IsReaderIdle
        {
            get { return IsIdle(IdleStatus.ReaderIdle); }
        }

        /// <inheritdoc/>
        public Boolean IsWriterIdle
        {
            get { return IsIdle(IdleStatus.WriterIdle); }
        }

        /// <inheritdoc/>
        public Boolean IsBothIdle
        {
            get { return IsIdle(IdleStatus.BothIdle); }
        }

        /// <inheritdoc/>
        public Int32 GetIdleCount(IdleStatus status)
        {
            if (Config.GetIdleTime(status) == 0)
            {
                switch (status)
                {
                    case IdleStatus.BothIdle:
                        Interlocked.Exchange(ref _idleCountForBoth, 0);
                        break;
                    case IdleStatus.ReaderIdle:
                        Interlocked.Exchange(ref _idleCountForRead, 0);
                        break;
                    case IdleStatus.WriterIdle:
                        Interlocked.Exchange(ref _idleCountForWrite, 0);
                        break;
                }
            }

            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite;
                default:
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <inheritdoc/>
        public Int32 BothIdleCount
        {
            get { return GetIdleCount(IdleStatus.BothIdle); }
        }

        /// <inheritdoc/>
        public Int32 ReaderIdleCount
        {
            get { return GetIdleCount(IdleStatus.ReaderIdle); }
        }

        /// <inheritdoc/>
        public Int32 WriterIdleCount
        {
            get { return GetIdleCount(IdleStatus.WriterIdle); }
        }

        /// <inheritdoc/>
        public DateTime GetLastIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _lastIdleTimeForBoth;
                case IdleStatus.ReaderIdle:
                    return _lastIdleTimeForRead;
                case IdleStatus.WriterIdle:
                    return _lastIdleTimeForWrite;
                default:
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <inheritdoc/>
        public DateTime LastBothIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.BothIdle); }
        }

        /// <inheritdoc/>
        public DateTime LastReaderIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.ReaderIdle); }
        }

        /// <inheritdoc/>
        public DateTime LastWriterIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.WriterIdle); }
        }

        /// <summary>
        /// Increases idle count.
        /// </summary>
        /// <param name="status">the <see cref="IdleStatus"/></param>
        /// <param name="currentTime">the time</param>
        public void IncreaseIdleCount(IdleStatus status, DateTime currentTime)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    Interlocked.Increment(ref _idleCountForBoth);
                    _lastIdleTimeForBoth = currentTime;
                    break;
                case IdleStatus.ReaderIdle:
                    Interlocked.Increment(ref _idleCountForRead);
                    _lastIdleTimeForRead = currentTime;
                    break;
                case IdleStatus.WriterIdle:
                    Interlocked.Increment(ref _idleCountForWrite);
                    _lastIdleTimeForWrite = currentTime;
                    break;
                default:
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <summary>
        /// Increases read bytes.
        /// </summary>
        /// <param name="increment">the amount to increase</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseReadBytes(Int64 increment, DateTime currentTime)
        {
            if (increment <= 0)
                return;

            _readBytes += increment;
            _lastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            this.Service.Statistics.IncreaseReadBytes(increment, currentTime);
        }

        /// <summary>
        /// Increases read messages.
        /// </summary>
        /// <param name="currentTime">the time</param>
        public void IncreaseReadMessages(DateTime currentTime)
        {
            _readMessages++;
            _lastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            this.Service.Statistics.IncreaseReadMessages(currentTime);
        }

        /// <summary>
        /// Increases written bytes.
        /// </summary>
        /// <param name="increment">the amount to increase</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseWrittenBytes(Int32 increment, DateTime currentTime)
        {
            if (increment <= 0)
                return;

            _writtenBytes += increment;
            _lastWriteTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForWrite, 0);

            this.Service.Statistics.IncreaseWrittenBytes(increment, currentTime);
            IncreaseScheduledWriteBytes(-increment);
        }

        /// <summary>
        /// Increases written messages.
        /// </summary>
        /// <param name="request">the request written</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseWrittenMessages(IWriteRequest request, DateTime currentTime)
        {
            IoBuffer buf = request.Message as IoBuffer;
            if (buf != null && buf.HasRemaining)
                return;

            _writtenMessages++;
            _lastWriteTime = currentTime;

            this.Service.Statistics.IncreaseWrittenMessages(currentTime);
            DecreaseScheduledWriteMessages();
        }

        /// <summary>
        /// Increase the number of scheduled write bytes for the session.
        /// </summary>
        /// <param name="increment">the number of newly added bytes to write</param>
        public void IncreaseScheduledWriteBytes(Int32 increment)
        {
            Interlocked.Add(ref _scheduledWriteBytes, increment);
            this.Service.Statistics.IncreaseScheduledWriteBytes(increment);
        }

        public void IncreaseScheduledWriteMessages()
        {
            Interlocked.Increment(ref _scheduledWriteMessages);
            this.Service.Statistics.IncreaseScheduledWriteMessages();
        }

        public void DecreaseScheduledWriteMessages()
        {
            Interlocked.Decrement(ref _scheduledWriteMessages);
            this.Service.Statistics.DecreaseScheduledWriteMessages();
        }

        /// <inheritdoc/>
        public void UpdateThroughput(DateTime currentTime, Boolean force)
        {
            Int64 interval = (Int64)(currentTime - _lastThroughputCalculationTime).TotalMilliseconds;

            Int64 minInterval = Config.ThroughputCalculationIntervalInMillis;
            if ((minInterval == 0 || interval < minInterval) && !force)
                return;

            _readBytesThroughput = (_readBytes - _lastReadBytes) * 1000.0 / interval;
            _writtenBytesThroughput = (_writtenBytes - _lastWrittenBytes) * 1000.0 / interval;
            _readMessagesThroughput = (_readMessages - _lastReadMessages) * 1000.0 / interval;
            _writtenMessagesThroughput = (_writtenMessages - _lastWrittenMessages) * 1000.0 / interval;

            _lastReadBytes = _readBytes;
            _lastWrittenBytes = _writtenBytes;
            _lastReadMessages = _readMessages;
            _lastWrittenMessages = _writtenMessages;

            _lastThroughputCalculationTime = currentTime;
        }

        private static void ResetCounter(Object sender, IoFutureEventArgs e)
        {
            AbstractIoSession session = (AbstractIoSession)e.Future.Session;
            Interlocked.Exchange(ref session._scheduledWriteBytes, 0);
            Interlocked.Exchange(ref session._scheduledWriteMessages, 0);
            session._readBytesThroughput = 0;
            session._readMessagesThroughput = 0;
            session._writtenBytesThroughput = 0;
            session._writtenMessagesThroughput = 0;
        }

        #endregion

        /// <summary>
        /// Fires a <see cref="IoEventType.SessionIdle"/> event to any applicable sessions in the specified collection.
        /// </summary>
        /// <param name="sessions"></param>
        /// <param name="currentTime"></param>
        public static void NotifyIdleness(IEnumerable<IoSession> sessions, DateTime currentTime)
        {
            foreach (IoSession s in sessions)
            {
                if (!s.CloseFuture.Closed)
                {
                    NotifyIdleSession(s, currentTime);
                }
            }
        }

        /// <summary>
        /// Fires a <see cref="IoEventType.SessionIdle"/> event if applicable for the
        /// specified <see cref="IoSession"/>.
        /// </summary>
        public static void NotifyIdleSession(IoSession session, DateTime currentTime)
        {
            NotifyIdleSession(session, currentTime, IdleStatus.BothIdle, session.LastIoTime);
            NotifyIdleSession(session, currentTime, IdleStatus.ReaderIdle, session.LastReadTime);
            NotifyIdleSession(session, currentTime, IdleStatus.WriterIdle, session.LastWriteTime);
            NotifyWriteTimeout(session, currentTime);
        }

        private static void NotifyIdleSession(IoSession session, DateTime currentTime, IdleStatus status, DateTime lastIoTime)
        {
            Int64 idleTime = session.Config.GetIdleTimeInMillis(status);
            if (idleTime > 0)
            {
                DateTime lastIdleTime = session.GetLastIdleTime(status);
                if (lastIoTime < lastIdleTime)
                    lastIoTime = lastIdleTime;

                if ((currentTime - lastIoTime).TotalMilliseconds >= idleTime)
                    session.FilterChain.FireSessionIdle(status);
            }
        }

        private static void NotifyWriteTimeout(IoSession session, DateTime currentTime)
        {
            Int64 writeTimeout = session.Config.WriteTimeoutInMillis;
            if ((writeTimeout > 0) && ((currentTime - session.LastWriteTime).TotalMilliseconds >= writeTimeout)
                    && !session.WriteRequestQueue.IsEmpty(session))
            {
                IWriteRequest request = session.CurrentWriteRequest;
                if (request != null)
                {
                    session.CurrentWriteRequest = null;
                    WriteTimeoutException cause = new WriteTimeoutException(request);
                    request.Future.Exception = cause;
                    session.FilterChain.FireExceptionCaught(cause);
                    // WriteException is an IOException, so we close the session.
                    session.Close(true);
                }
            }
        }

        /// <summary>
        /// A queue which handles the CLOSE request.
        /// </summary>
        class CloseAwareWriteQueue : IWriteRequestQueue
        {
            private readonly AbstractIoSession _session;
            private readonly IWriteRequestQueue _queue;

            /// <summary>
            /// </summary>
            public CloseAwareWriteQueue(AbstractIoSession session, IWriteRequestQueue queue)
            {
                _session = session;
                _queue = queue;
            }

            /// <inheritdoc/>
            public Int32 Size
            {
                get { return _queue.Size; }
            }

            /// <inheritdoc/>
            public IWriteRequest Poll(IoSession session)
            {
                IWriteRequest answer = _queue.Poll(session);
                if (Object.ReferenceEquals(answer, CLOSE_REQUEST))
                {
                    _session.Close(true);
                    Dispose(_session);
                    answer = null;
                }
                return answer;
            }

            /// <inheritdoc/>
            public void Offer(IoSession session, IWriteRequest writeRequest)
            {
                _queue.Offer(session, writeRequest);
            }

            /// <inheritdoc/>
            public Boolean IsEmpty(IoSession session)
            {
                return _queue.IsEmpty(session);
            }

            /// <inheritdoc/>
            public void Clear(IoSession session)
            {
                _queue.Clear(session);
            }

            /// <inheritdoc/>
            public void Dispose(IoSession session)
            {
                _queue.Dispose(session);
            }
        }
    }
}
