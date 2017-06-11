using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Util;
#if UNITY
using Interlocked = Mina.Util.InterlockedUtil;
#endif

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IoFuture"/> associated with an <see cref="IoSession"/>.
    /// </summary>
    public class DefaultIoFuture : IoFuture, IDisposable
    {
        private readonly IoSession _session;
        private volatile Boolean _ready;
#if NET20
        private readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);
#else
        private readonly ManualResetEventSlim _readyEvent = new ManualResetEventSlim(false);
#endif
        private Object _value;
        private EventHandler<IoFutureEventArgs> _complete;
        private Boolean _disposed;

        private readonly object _syncRoot = new byte[0];

        /// <summary>
        /// </summary>
        public DefaultIoFuture(IoSession session)
        {
            _session = session;
        }

        /// <inheritdoc/>
        public event EventHandler<IoFutureEventArgs> Complete
        {
            add
            {
                EventHandler<IoFutureEventArgs> tmp;
                EventHandler<IoFutureEventArgs> complete = _complete;
                do
                {
                    tmp = complete;
                    EventHandler<IoFutureEventArgs> newComplete = (EventHandler<IoFutureEventArgs>)Delegate.Combine(tmp, value);
                    complete = Interlocked.CompareExchange(ref _complete, newComplete, tmp);
                }
                while (complete != tmp);

                if (_ready)
                    OnComplete(value);
            }
            remove
            {
                EventHandler<IoFutureEventArgs> tmp;
                EventHandler<IoFutureEventArgs> complete = _complete;
                do
                {
                    tmp = complete;
                    EventHandler<IoFutureEventArgs> newComplete = (EventHandler<IoFutureEventArgs>)Delegate.Remove(tmp, value);
                    complete = Interlocked.CompareExchange(ref _complete, newComplete, tmp);
                }
                while (complete != tmp);
            }
        }

        /// <inheritdoc/>
        public virtual IoSession Session
        {
            get { return _session; }
        }

        /// <inheritdoc/>
        public Boolean Done
        {
            get { return _ready; }
        }

        /// <summary>
        /// Gets or sets the value associated with this future.
        /// </summary>
        public Object Value
        {
            get { return _value; }
            set
            {
                lock (_syncRoot)
                {
                    if (_ready)
                        return;
                    _ready = true;
                    _value = value;
                    _readyEvent.Set();
                }
                OnComplete();
            }
        }

        /// <summary>
        /// Sets the result of the asynchronous operation, and mark it as finished.
        /// </summary>
        /// <param name="value">the result to store into the future</param>
        /// <returns>
        /// <code>true</code> if the value has been set,
        /// <code>false</code> if the future already has a value (thus is in ready state).
        /// </returns>
        public Boolean SetValue(Object value)
        {
            lock (_syncRoot)
            {
                if (_ready)
                    return false;
                _ready = true;
                _value = value;
                _readyEvent.Set();
            }
            OnComplete();
            return true;
        }

        /// <inheritdoc/>
        public IoFuture Await()
        {
            Await0(Timeout.Infinite);
            return this;
        }

        /// <inheritdoc/>
        public Boolean Await(Int32 millisecondsTimeout)
        {
            return Await0(millisecondsTimeout);
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
            if (!_disposed)
            {
                if (disposing)
                {
                    ((IDisposable)_readyEvent).Dispose();
                    _disposed = true;
                }
            }
        }

        private Boolean Await0(Int32 millisecondsTimeout)
        {
            if (_ready)
                return _ready;
#if NET20
            _readyEvent.WaitOne(millisecondsTimeout);
            if (_ready)
                _readyEvent.Close();
#else
            _readyEvent.Wait(millisecondsTimeout);
            if (_ready)
                _readyEvent.Dispose();
#endif

            return _ready;
        }

        private void OnComplete()
        {
            EventHandler<IoFutureEventArgs> complete = _complete;
            if (complete != null)
            {
                Delegate[] handlers = complete.GetInvocationList();
                foreach (var current in handlers)
                {
                    OnComplete((EventHandler<IoFutureEventArgs>)current);
                }
            }
        }

        private void OnComplete(EventHandler<IoFutureEventArgs> act)
        {
            try
            {
                act(_session, new IoFutureEventArgs(this));
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }
    }
}
