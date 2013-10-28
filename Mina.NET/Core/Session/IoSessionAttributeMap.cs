using System;
using System.Collections.Generic;

namespace Mina.Core.Session
{
    /// <summary>
    /// Stores the user-defined attributes which is provided per <see cref="IoSession"/>.
    /// All user-defined attribute accesses in <see cref="IoSession"/> are forwarded to
    /// the instance of <see cref="IoSessionAttributeMap"/>.
    /// </summary>
    public interface IoSessionAttributeMap
    {
        /// <summary>
        /// Returns the value of user defined attribute associated with the
        /// specified key. If there's no such attribute, the specified default
        /// value is associated with the specified key, and the default value is
        /// returned.
        /// </summary>
        Object GetAttribute(IoSession session, Object key, Object defaultValue);
        /// <summary>
        /// Sets a user-defined attribute.
        /// </summary>
        Object SetAttribute(IoSession session, Object key, Object value);
        /// <summary>
        /// Sets a user defined attribute if the attribute with the specified key
        /// is not set yet.
        /// </summary>
        Object SetAttributeIfAbsent(IoSession session, Object key, Object value);
        /// <summary>
        /// Removes a user-defined attribute with the specified key.
        /// </summary>
        Object RemoveAttribute(IoSession session, Object key);
        /// <summary>
        /// Returns <tt>true</tt> if this session contains the attribute with the specified <tt>key</tt>.
        /// </summary>
        Boolean ContainsAttribute(IoSession session, Object key);
        /// <summary>
        /// Returns the keys of all user-defined attributes.
        /// </summary>
        IEnumerable<Object> GetAttributeKeys(IoSession session);
        /// <summary>
        /// Disposes any releases associated with the specified session.
        /// This method is invoked on disconnection.
        /// </summary>
        void Dispose(IoSession session);
    }
}
