using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IoFuture"/> associated with an <see cref="IoSession"/>.
    /// </summary>
    public class DefaultIoFuture : IoFuture
    {
        private readonly IoSession _session;
        private volatile Boolean _ready;
        private readonly ManualResetEventSlim _readyEvent = new ManualResetEventSlim(false);
        private Object _value;
        private Action<IoFuture> _complete;

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

        private Boolean Await0(Int32 millisecondsTimeout)
        {
            if (_ready)
                return _ready;

            _readyEvent.Wait(millisecondsTimeout);
            if (_ready)
                _readyEvent.Dispose();

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
