using System;
using System.Collections;
using System.Collections.Generic;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// The default implementation of <see cref="IoFilterChainBuilder"/> which is useful
    /// in most cases.  <see cref="DefaultIoFilterChainBuilder"/> has an identical interface
    /// with <see cref="IoFilter"/>; it contains a list of <see cref="IoFilter"/>s that you can
    /// modify. The <see cref="IoFilter"/>s which are added to this builder will be appended
    /// to the <see cref="IoFilterChain"/> when BuildFilterChain(IoFilterChain) is
    /// invoked.
    /// However, the identical interface doesn't mean that it behaves in an exactly
    /// same way with <see cref="IoFilterChain"/>.  <see cref="DefaultIoFilterChainBuilder"/>
    /// doesn't manage the life cycle of the <see cref="IoFilter"/>s at all, and the
    /// existing <see cref="Core.Session.IoSession"/>s won't get affected by the changes in this builder.
    /// <see cref="IoFilterChainBuilder"/>s affect only newly created <see cref="Core.Session.IoSession"/>s.
    /// </summary>
    public class DefaultIoFilterChainBuilder : IoFilterChainBuilder
    {
        private readonly List<EntryImpl> _entries;
        private readonly Object _syncRoot;

        /// <summary>
        /// </summary>
        public DefaultIoFilterChainBuilder()
        {
            _entries = new List<EntryImpl>();
            _syncRoot = ((ICollection)_entries).SyncRoot;
        }

        /// <summary>
        /// </summary>
        public DefaultIoFilterChainBuilder(DefaultIoFilterChainBuilder filterChain)
        {
            if (filterChain == null)
                throw new ArgumentNullException("filterChain");
            _entries = new List<EntryImpl>(filterChain._entries);
            _syncRoot = ((ICollection)_entries).SyncRoot;
        }

        /// <summary>
        /// Gets the <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name we are looking for</param>
        /// <returns>the <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/> with the given name, or null if not found</returns>
        public IEntry<IoFilter, INextFilter> GetEntry(String name)
        {
            return _entries.Find(e => e.Name.Equals(name));
        }

        /// <summary>
        /// Gets the <see cref="IoFilter"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>the <see cref="IoFilter"/>, or null if not found</returns>
        public IoFilter Get(String name)
        {
            IEntry<IoFilter, INextFilter> entry = GetEntry(name);
            return entry == null ? null : entry.Filter;
        }

        /// <summary>
        /// Gets all <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/>s in this chain.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEntry<IoFilter, INextFilter>> GetAll()
        {
            foreach (EntryImpl item in _entries)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Checks if this chain contains a filter with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>true if this chain contains a filter with the specified <paramref name="name"/></returns>
        public Boolean Contains(String name)
        {
            return GetEntry(name) != null;
        }

        /// <summary>
        /// Adds the specified filter with the specified name at the beginning of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddFirst(String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                Register(0, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name at the end of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddLast(String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                Register(_entries.Count, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name just before the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddBefore(String baseName, String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);

                Int32 i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                    Register(i, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name just after the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddAfter(String baseName, String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);
                Int32 i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                    Register(i + 1, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Removes the filter with the specified name from this chain.
        /// </summary>
        /// <param name="name">the name of the filter to remove</param>
        /// <returns>the removed filter</returns>
        public IoFilter Remove(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            lock (_syncRoot)
            {
                EntryImpl entry = _entries.Find(e => e.Name.Equals(name));
                if (entry != null)
                {
                    _entries.Remove(entry);
                    return entry.Filter;
                }
            }

            throw new ArgumentException("Unknown filter name: " + name);
        }

        /// <summary>
        /// Replace the filter with the specified name with the specified new filter.
        /// </summary>
        /// <param name="name">the name of the filter to replace</param>
        /// <param name="newFilter">the new filter</param>
        /// <returns>the old filter</returns>
        public IoFilter Replace(String name, IoFilter newFilter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(name);
                EntryImpl e = (EntryImpl)GetEntry(name);
                IoFilter oldFilter = e.Filter;
                e.Filter = newFilter;
                return oldFilter;
            }
        }

        /// <summary>
        /// Removes all filters added to this chain.
        /// </summary>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
            }
        }

        /// <inheritdoc/>
        public void BuildFilterChain(IoFilterChain chain)
        {
            foreach (EntryImpl entry in _entries)
            {
                chain.AddLast(entry.Name, entry.Filter);
            }
        }

        private void CheckBaseName(String baseName)
        {
            if (baseName == null)
                throw new ArgumentNullException("baseName");
            if (!Contains(baseName))
                throw new ArgumentException("Unknown filter name: " + baseName);
        }

        private void Register(Int32 index, EntryImpl e)
        {
            if (Contains(e.Name))
                throw new ArgumentException("Other filter is using the same name: " + e.Name);
            _entries.Insert(index, e);
        }

        class EntryImpl : IEntry<IoFilter, INextFilter>
        {
            private readonly DefaultIoFilterChainBuilder _chain;
            private readonly String _name;
            private IoFilter _filter;

            public EntryImpl(DefaultIoFilterChainBuilder chain, String name, IoFilter filter)
            {
                if (name == null)
                    throw new ArgumentNullException("name");
                if (filter == null)
                    throw new ArgumentNullException("filter");

                _chain = chain;
                _name = name;
                _filter = filter;
            }

            public String Name
            {
                get { return _name; }
            }

            public IoFilter Filter
            {
                get { return _filter; }
                set { _filter = value; }
            }

            public INextFilter NextFilter
            {
                get { throw new InvalidOperationException(); }
            }

            public override String ToString()
            {
                return "(" + _name + ':' + _filter + ')';
            }

            public void AddAfter(String name, IoFilter filter)
            {
                _chain.AddAfter(Name, name, filter);
            }

            public void AddBefore(String name, IoFilter filter)
            {
                _chain.AddBefore(Name, name, filter);
            }

            public void Remove()
            {
                _chain.Remove(Name);
            }

            public void Replace(IoFilter newFilter)
            {
                _chain.Replace(Name, newFilter);
            }
        }
    }
}
