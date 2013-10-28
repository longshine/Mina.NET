using System;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// Represents a name-filter pair that an <see cref="IoFilterChain"/> contains.
    /// </summary>
    public interface IEntry
    {
        /// <summary>
        /// Gets the name of the filter.
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Gets the filter.
        /// </summary>
        IoFilter Filter { get; }
        /// <summary>
        /// Gets the <see cref="INextFilter"/> of the filter.
        /// </summary>
        INextFilter NextFilter { get; }
        /// <summary>
        /// Adds the specified filter with the specified name just before this entry.
        /// </summary>
        void AddBefore(String name, IoFilter filter);
        /// <summary>
        /// Adds the specified filter with the specified name just after this entry.
        /// </summary>
        void AddAfter(String name, IoFilter filter);
        /// <summary>
        /// Replace the filter of this entry with the specified new filter.
        /// </summary>
        void Replace(IoFilter newFilter);
        /// <summary>
        /// Removes this entry from the chain it belongs to.
        /// </summary>
        void Remove();
    }
}
