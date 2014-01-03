using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Util;

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
        private Action<IoFuture> _complete;
        private Boolean _disposed;

        public DefaultIoFuture(IoSession session)
        {
            _session = session;
        }

        public event Action<IoFuture> Complete
        {
            add
            {
                Action<IoFuture> tmp;
                Action<IoFuture> complete = _complete;
                do
                {
                    tmp = complete;
                    Action<IoFuture> newComplete = (Action<IoFuture>)Delegate.Combine(tmp, value);
                    complete = Interlocked.CompareExchange(ref _complete, newComplete, tmp);
                }
                while (complete != tmp);

                if (_ready)
                    OnComplete(value);
            }
            remove
            {
                Action<IoFuture> tmp;
                Action<IoFuture> complete = _complete;
                do
                {
                    tmp = complete;
                    Action<IoFuture> newComplete = (Action<IoFuture>)Delegate.Remove(tmp, value);
                    complete = Interlocked.CompareExchange(ref _complete, newComplete, tmp);
                }
                while (complete != tmp);
            }
        }

        public virtual IoSession Session
        {
            get { return _session; }
        }

        public Boolean Done
        {
            get { return _ready; }
        }

        public Object Value
        {
            get { return _value; }
            set
            {
                lock (this)
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

        public IoFuture Await()
        {
            Await0(Timeout.Infinite);
            return this;
        }

        public Boolean Await(Int32 millisecondsTimeout)
        {
            return Await0(millisecondsTimeout);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
            Action<IoFuture> complete = _complete;
            if (complete != null)
            {
                Delegate[] handlers = complete.GetInvocationList();
                foreach (var current in handlers)
                {
                    OnComplete((Action<IoFuture>)current);
                }
            }
        }

        private void OnComplete(Action<IoFuture> act)
        {
            try
            {
                act(this);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }
    }
}
