using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mina.Core.Session
{
    class DefaultIoSessionAttributeMap : IoSessionAttributeMap
    {
        private readonly ConcurrentDictionary<Object, Object> _attributes = new ConcurrentDictionary<Object, Object>();

        public Object GetAttribute(IoSession session, Object key, Object defaultValue)
        {
            if (defaultValue == null)
            {
                Object obj;
                _attributes.TryGetValue(key, out obj);
                return obj;
            }
            else
            {
                return _attributes.GetOrAdd(key, defaultValue);
            }
        }

        public Object SetAttribute(IoSession session, Object key, Object value)
        {
            Object old = null;
            _attributes.AddOrUpdate(key, value, (k, v) => 
            {
                old = v;
                return value;
            });
            return old;
        }

        public Object SetAttributeIfAbsent(IoSession session, Object key, Object value)
        {
            return _attributes.GetOrAdd(key, value);
        }

        public Object RemoveAttribute(IoSession session, Object key)
        {
            Object obj;
            _attributes.TryRemove(key, out obj);
            return obj;
        }

        public Boolean ContainsAttribute(IoSession session, Object key)
        {
            return _attributes.ContainsKey(key);
        }

        public IEnumerable<Object> GetAttributeKeys(IoSession session)
        {
            return _attributes.Keys;
        }

        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
