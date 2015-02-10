using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// Creates a Key from a class name and an attribute name. The resulting Key will
    /// be stored in the session Map.
    /// </summary>
    [Serializable]
    public sealed class AttributeKey
    {
        private readonly String _name;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="source">the class this AttributeKey will be attached to</param>
        /// <param name="name">the Attribute name</param>
        public AttributeKey(Type source, String name)
        {
            _name = source.Name + "." + name + "@" + base.GetHashCode().ToString("X");
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return _name;
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            int h = 17 * 37 + ((_name == null) ? 0 : _name.GetHashCode());
            return h;
        }

        /// <inheritdoc/>
        public override Boolean Equals(Object obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;
            AttributeKey other = obj as AttributeKey;
            if (other == null)
                return false;
            return _name.Equals(other._name);
        }
    }
}
