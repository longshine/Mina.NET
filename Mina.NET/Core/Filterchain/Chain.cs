using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Mina.Core.Filterchain
{
    public interface IChain<TFilter, TNextFilter>
    {
        IEntry<TFilter, TNextFilter> GetEntry(String name);
        IEntry<TFilter, TNextFilter> GetEntry(TFilter filter);
        IEntry<TFilter, TNextFilter> GetEntry(Type filterType);
        IEntry<TFilter, TNextFilter> GetEntry<T>() where T : TFilter;
        TFilter Get(String name);
        TNextFilter GetNextFilter(String name);
        TNextFilter GetNextFilter(TFilter filter);
        TNextFilter GetNextFilter(Type filterType);
        TNextFilter GetNextFilter<T>() where T : TFilter;
        IEnumerable<IEntry<TFilter, TNextFilter>> GetAll();
        Boolean Contains(String name);
        Boolean Contains(TFilter filter);
        Boolean Contains(Type filterType);
        Boolean Contains<T>() where T : TFilter;
        void AddFirst(String name, TFilter filter);
        void AddLast(String name, TFilter filter);
        void AddBefore(String baseName, String name, TFilter filter);
        void AddAfter(String baseName, String name, TFilter filter);
        TFilter Replace(String name, TFilter newFilter);
        void Replace(TFilter oldFilter, TFilter newFilter);
        TFilter Remove(String name);
        void Remove(TFilter filter);
        void Clear();
    }

    public abstract class Chain<TChain, TFilter, TNextFilter> : IChain<TFilter, TNextFilter>
        where TChain : Chain<TChain, TFilter, TNextFilter>
    {
        private readonly IDictionary<String, Entry> _name2entry = new ConcurrentDictionary<String, Entry>();
        protected readonly Entry _head;
        protected readonly Entry _tail;
        private readonly Func<TFilter, TFilter, Boolean> _equalsFunc;
        private readonly Func<TChain, Entry, Entry, String, TFilter, Entry> _entryFactory;

        protected Chain(Func<Entry, TNextFilter> nextFilterFactory, Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory)
            : this((chain, prev, next, name, filter) => new Entry(chain, prev, next, name, filter, nextFilterFactory),
            headFilterFactory, tailFilterFactory)
        { }

        protected Chain(Func<TChain, Entry, Entry, String, TFilter, Entry> entryFactory,
            Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory)
            : this(entryFactory, headFilterFactory, tailFilterFactory, (t1, t2) => Object.ReferenceEquals(t1, t2))
        { }

        protected Chain(Func<TChain, Entry, Entry, String, TFilter, Entry> entryFactory, 
            Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory,
            Func<TFilter, TFilter, Boolean> equalsFunc)
        {
            _equalsFunc = equalsFunc;
            _entryFactory = entryFactory;
            _head = entryFactory((TChain)this, null, null, "head", headFilterFactory());
            _tail = entryFactory((TChain)this, _head, null, "tail", tailFilterFactory());
            _head._nextEntry = _tail;
        }

        public IEntry<TFilter, TNextFilter> GetEntry(String name)
        {
            Entry e;
            _name2entry.TryGetValue(name, out e);
            return e;
        }

        public TFilter Get(String name)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(name);
            return e == null ? default(TFilter) : e.Filter;
        }

        public IEntry<TFilter, TNextFilter> GetEntry(TFilter filter)
        {
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                if (_equalsFunc(e.Filter, filter))
                    return e;
                e = e._nextEntry;
            }
            return null;
        }

        public IEntry<TFilter, TNextFilter> GetEntry(Type filterType)
        {
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                if (filterType.IsAssignableFrom(e.Filter.GetType()))
                    return e;
                e = e._nextEntry;
            }
            return null;
        }

#if NET20
        IEntry<TFilter, TNextFilter> IChain<TFilter, TNextFilter>.GetEntry<T>()
#else
        public IEntry<TFilter, TNextFilter> GetEntry<T>() where T : TFilter
#endif
        {
            Type filterType = typeof(T);
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                if (filterType.IsAssignableFrom(e.Filter.GetType()))
                    return e;
                e = e._nextEntry;
            }
            return null;
        }

        public TNextFilter GetNextFilter(String name)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(name);
            return e == null ? default(TNextFilter) : e.NextFilter;
        }

        public TNextFilter GetNextFilter(TFilter filter)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(filter);
            return e == null ? default(TNextFilter) : e.NextFilter;
        }

        public TNextFilter GetNextFilter(Type filterType)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(filterType);
            return e == null ? default(TNextFilter) : e.NextFilter;
        }
        
#if NET20
        TNextFilter IChain<TFilter, TNextFilter>.GetNextFilter<T>()
        {
            IEntry<TFilter, TNextFilter> e = ((IChain<TFilter, TNextFilter>)this).GetEntry<T>();
#else
        public TNextFilter GetNextFilter<T>() where T : TFilter
        {
            IEntry<TFilter, TNextFilter> e = GetEntry<T>();
#endif
            return e == null ? default(TNextFilter) : e.NextFilter;
        }

        public IEnumerable<IEntry<TFilter, TNextFilter>> GetAll()
        {
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                yield return e;
                e = e._nextEntry;
            }
        }

        public Boolean Contains(String name)
        {
            return GetEntry(name) != null;
        }

        public Boolean Contains(TFilter filter)
        {
            return GetEntry(filter) != null;
        }

        public Boolean Contains(Type filterType)
        {
            return GetEntry(filterType) != null;
        }
        
#if NET20
        Boolean IChain<TFilter, TNextFilter>.Contains<T>()
        {
            return ((IChain<TFilter, TNextFilter>)this).GetEntry<T>() != null;
        }
#else
        public Boolean Contains<T>() where T : TFilter
        {
            return GetEntry<T>() != null;
        }
#endif

        public void AddFirst(String name, TFilter filter)
        {
            CheckAddable(name);
            Register(_head, name, filter);
        }

        public void AddLast(String name, TFilter filter)
        {
            CheckAddable(name);
            Register(_tail._prevEntry, name, filter);
        }

        public void AddBefore(String baseName, String name, TFilter filter)
        {
            Entry baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry._prevEntry, name, filter);
        }

        public void AddAfter(String baseName, String name, TFilter filter)
        {
            Entry baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry, name, filter);
        }

        public TFilter Replace(String name, TFilter newFilter)
        {
            Entry entry = CheckOldName(name);
            TFilter oldFilter = entry.Filter;
            entry.Filter = newFilter;
            return oldFilter;
        }

        public void Replace(TFilter oldFilter, TFilter newFilter)
        {
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                if (_equalsFunc(e.Filter, oldFilter))
                {
                    e.Filter = newFilter;
                    return;
                }
                e = e._nextEntry;
            }
            throw new ArgumentException("Filter not found: " + oldFilter.GetType().Name);
        }

        public TFilter Remove(String name)
        {
            Entry entry = CheckOldName(name);
            Deregister(entry);
            return entry.Filter;
        }

        public void Remove(TFilter filter)
        {
            Entry e = _head._nextEntry;
            while (e != _tail)
            {
                if (_equalsFunc(e.Filter, filter))
                {
                    Deregister(e);
                    return;
                }
                e = e._nextEntry;
            }
            throw new ArgumentException("Filter not found: " + filter.GetType().Name);
        }

        public void Clear()
        {
            foreach (var entry in _name2entry.Values)
            {
                Deregister((Entry)entry);
            }
        }

        private void CheckAddable(String name)
        {
            if (_name2entry.ContainsKey(name))
                throw new ArgumentException("Other filter is using the same name '" + name + "'");
        }

        private Entry CheckOldName(String baseName)
        {
            return (Entry)_name2entry[baseName];
        }

        private void Register(Entry prevEntry, String name, TFilter filter)
        {
            Entry newEntry = _entryFactory((TChain)this, prevEntry, prevEntry._nextEntry, name, filter);

            OnPreAdd(newEntry);

            prevEntry._nextEntry._prevEntry = newEntry;
            prevEntry._nextEntry = newEntry;
            _name2entry.Add(name, newEntry);

            OnPostAdd(newEntry);
        }

        private void Deregister(Entry entry)
        {
            OnPreRemove(entry);
            Deregister0(entry);
            OnPostRemove(entry);
        }

        protected void Deregister0(Entry entry)
        {
            Entry prevEntry = entry._prevEntry;
            Entry nextEntry = entry._nextEntry;
            prevEntry._nextEntry = nextEntry;
            nextEntry._prevEntry = prevEntry;

            _name2entry.Remove(entry.Name);
        }

        protected virtual void OnPreAdd(Entry entry) { }

        protected virtual void OnPostAdd(Entry entry) { }

        protected virtual void OnPreRemove(Entry entry) { }

        protected virtual void OnPostRemove(Entry entry) { }

        public class Entry : IEntry<TFilter, TNextFilter>
        {
            private readonly TChain _chain;
            private readonly String _name;
            internal protected Entry _prevEntry;
            internal protected Entry _nextEntry;
            private TFilter _filter;
            private readonly TNextFilter _nextFilter;

            public Entry(TChain chain, Entry prevEntry, Entry nextEntry,
                String name, TFilter filter, Func<Entry, TNextFilter> nextFilterFactory)
            {
                if (filter == null)
                    throw new ArgumentNullException("filter");
                if (name == null)
                    throw new ArgumentNullException("name");

                _chain = chain;
                _prevEntry = prevEntry;
                _nextEntry = nextEntry;
                _name = name;
                _filter = filter;
                _nextFilter = nextFilterFactory(this);
            }

            public String Name
            {
                get { return _name; }
            }

            public TFilter Filter
            {
                get { return _filter; }
                set
                {
                    if (value == null)
                        throw new ArgumentNullException("value");
                    _filter = value;
                }
            }

            public TNextFilter NextFilter
            {
                get { return _nextFilter; }
            }

            public TChain Chain
            {
                get { return _chain; }
            }

            public Entry PrevEntry
            {
                get { return _prevEntry; }
            }

            public Entry NextEntry
            {
                get { return _nextEntry; }
            }

            public void AddBefore(String name, TFilter filter)
            {
                _chain.AddBefore(Name, name, filter);
            }

            public void AddAfter(String name, TFilter filter)
            {
                _chain.AddAfter(Name, name, filter);
            }

            public void Replace(TFilter newFilter)
            {
                _chain.Replace(Name, newFilter);
            }

            public void Remove()
            {
                _chain.Remove(Name);
            }

            public override String ToString()
            {
                StringBuilder sb = new StringBuilder();

                // Add the current filter
                sb.Append("('").Append(Name).Append('\'');

                // Add the previous filter
                sb.Append(", prev: '");

                if (_prevEntry != null)
                {
                    sb.Append(_prevEntry.Name);
                    sb.Append(':');
                    sb.Append(_prevEntry.Filter.GetType().Name);
                }
                else
                {
                    sb.Append("null");
                }

                // Add the next filter
                sb.Append("', next: '");

                if (_nextEntry != null)
                {
                    sb.Append(_nextEntry.Name);
                    sb.Append(':');
                    sb.Append(_nextEntry.Filter.GetType().Name);
                }
                else
                {
                    sb.Append("null");
                }

                sb.Append("')");
                return sb.ToString();
            }
        }
    }
}
