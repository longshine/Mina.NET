using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Mina.Util
{
    class ExpiringMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public const Int32 DefaultTimeToLive = 60;
        public const Int32 DefaultExpirationInterval = 1;

        private static volatile Int32 expirerCount = 1;
        private readonly ConcurrentDictionary<TKey, ExpiringObject> _dict;
        private readonly ReaderWriterLock _stateLock = new ReaderWriterLock();
        private Int32 _timeToLiveMillis;
        private Int32 _expirationIntervalMillis;
        private readonly Thread _expirerThread;
        private Boolean _running;

        public event EventHandler<ExpirationEventArgs<TValue>> Expired;

        public ExpiringMap()
            : this(DefaultTimeToLive, DefaultExpirationInterval)
        { }

        public ExpiringMap(Int32 timeToLive)
            : this(timeToLive, DefaultExpirationInterval)
        { }

        public ExpiringMap(Int32 timeToLive, Int32 expirationInterval)
        {
            _dict = new ConcurrentDictionary<TKey, ExpiringObject>();
            TimeToLive = timeToLive;
            ExpirationInterval = expirationInterval;
            _expirerThread = new Thread(Expiring);
            _expirerThread.Name = "ExpiringMapExpirer-" + expirerCount++;
            _expirerThread.IsBackground = true;
        }

        public Int32 TimeToLive
        {
            get
            {
                Int32 i;
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                i = _timeToLiveMillis / 1000;
                _stateLock.ReleaseReaderLock();
                return i;
            }
            set
            {
                _stateLock.AcquireWriterLock(Timeout.Infinite);
                _timeToLiveMillis = value * 1000;
                _stateLock.ReleaseWriterLock();
            }
        }

        public Int32 ExpirationInterval
        {
            get
            {
                Int32 i;
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                i = _expirationIntervalMillis / 1000;
                _stateLock.ReleaseReaderLock();
                return i;
            }
            set
            {
                _stateLock.AcquireWriterLock(Timeout.Infinite);
                _expirationIntervalMillis = value * 1000;
                _stateLock.ReleaseWriterLock();
            }
        }

        public Boolean Running
        {
            get
            {
                Boolean running;
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                running = _running;
                _stateLock.ReleaseReaderLock();
                return running;
            }
        }

        public void StartExpiring()
        {
            if (Running)
                return;

            _stateLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (!_running)
                {
                    _running = true;
                    _expirerThread.Start();
                }
            }
            finally
            {
                _stateLock.ReleaseWriterLock();
            }
        }

        public void StopExpiring()
        {
            _stateLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_running)
                {
                    _running = false;
                    _expirerThread.Interrupt();
                }
            }
            finally
            {
                _stateLock.ReleaseWriterLock();
            }
        }

        private void Expiring()
        {
            while (_running)
            {
                ProcessExpires();
                try
                {
                    Thread.Sleep(_expirationIntervalMillis);
                }
                catch (ThreadInterruptedException)
                {
                    // do nothing
                }
            }
        }

        private void ProcessExpires()
        {
            DateTime now = DateTime.Now;
            ExpiringObject dummy;
            foreach (ExpiringObject o in _dict.Values)
            {
                if (_timeToLiveMillis <= 0)
                    continue;

                if ((now - o.LastAccessTime).TotalMilliseconds >= _timeToLiveMillis)
                {
                    _dict.TryRemove(o.Key, out dummy);
                    DelegateUtils.SafeInvoke(Expired, this, new ExpirationEventArgs<TValue>(o.Value));
                }
            }
        }   

        public void Add(TKey key, TValue value)
        {
            _dict.TryAdd(key, new ExpiringObject(key, value, DateTime.Now));
        }

        public Boolean ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dict.Keys; }
        }

        public Boolean Remove(TKey key)
        {
            ExpiringObject obj;
            return _dict.TryRemove(key, out obj);
        }

        public Boolean TryGetValue(TKey key, out TValue value)
        {
            ExpiringObject obj;
            if (_dict.TryGetValue(key, out obj))
            {
                value = obj.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get { throw new NotSupportedException(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                ExpiringObject obj = new ExpiringObject(key, value, DateTime.Now);
                _dict.AddOrUpdate(key, obj, (k, v) => obj);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public Boolean Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
        {
            throw new NotSupportedException();
        }

        public Int32 Count
        {
            get { return _dict.Count; }
        }

        public Boolean IsReadOnly
        {
            get { return false; }
        }

        public Boolean Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, ExpiringObject> pair in _dict)
            {
                yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class ExpiringObject
        {
            private readonly TKey _key;
            private readonly TValue _value;
            private DateTime _lastAccessTime;
            private readonly ReaderWriterLock _lastAccessTimeLock;

            public ExpiringObject(TKey key, TValue value, DateTime lastAccessTime)
            {
                _key = key;
                _value = value;
                _lastAccessTime = lastAccessTime;
                _lastAccessTimeLock = new ReaderWriterLock();
            }

            public TKey Key { get { return _key; } }

            public TValue Value { get { return _value; } }

            public DateTime LastAccessTime
            {
                get
                {
                    DateTime time;
                    _lastAccessTimeLock.AcquireReaderLock(Timeout.Infinite);
                    time = _lastAccessTime;
                    _lastAccessTimeLock.ReleaseReaderLock();
                    return time;
                }
                set
                {
                    _lastAccessTimeLock.AcquireWriterLock(Timeout.Infinite);
                    _lastAccessTime = value;
                    _lastAccessTimeLock.ReleaseWriterLock();
                }
            }

            public override Boolean Equals(Object obj)
            {
                return Object.Equals(_value, obj);
            }

            public override Int32 GetHashCode()
            {
                return _value.GetHashCode();
            }
        }
    }

    class ExpirationEventArgs<T> : EventArgs
    {
        private readonly T _obj;

        public ExpirationEventArgs(T obj)
        {
            _obj = obj;
        }

        public T Object { get { return _obj; } }
    }
}
