using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A container for data of a specific primitive type. 
    /// </summary>
    public abstract class Buffer
    {
        private Int32 _mark = -1;
        private Int32 _position = 0;
        private Int32 _limit;
        private Int32 _capacity;

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, and capacity,
        /// after checking invariants.
        /// </summary>
        protected Buffer(Int32 mark, Int32 pos, Int32 lim, Int32 cap)
        {
            if (cap < 0)
                throw new ArgumentException("Capacity should be >= 0", "cap");
            _capacity = cap;
            Limit = lim;
            Position = pos;
            if (mark >= 0)
            {
                if (mark > pos)
                    throw new ArgumentException("Invalid mark position", "mark");
                _mark = mark;
            }
        }

        /// <summary>
        /// Gets this buffer's capacity.
        /// </summary>
        public Int32 Capacity
        {
            get { return _capacity; }
        }

        /// <summary>
        /// Gets or sets this buffer's position.
        /// If the mark is defined and larger than the new position then it is discarded.
        /// </summary>
        public Int32 Position
        {
            get { return _position; }
            set
            {
                if ((value > _limit) || (value < 0))
                    throw new ArgumentException("Invalid position", "value");
                _position = value;
                if (_mark > _position) _mark = -1;
            }
        }

        /// <summary>
        /// Gets or sets this buffer's limit.
        /// If the position is larger than the new limit then it is set to the new limit.
        /// If the mark is defined and larger than the new limit then it is discarded.
        /// </summary>
        public Int32 Limit
        {
            get { return _limit; }
            set
            {
                if ((value > _capacity) || (value < 0))
                    throw new ArgumentException("Invalid limit", "value");
                _limit = value;
                if (_position > _limit) _position = _limit;
                if (_mark > _limit) _mark = -1;
            }
        }

        /// <summary>
        /// Gets the number of elements between the current position and the limit.
        /// </summary>
        public Int32 Remaining
        {
            get { return _limit - _position; }
        }

        /// <summary>
        /// Tells whether there are any elements between the current position and the limit.
        /// </summary>
        public Boolean HasRemaining
        {
            get { return _position < _limit; }
        }

        /// <summary>
        /// Tells whether or not this buffer is read-only.
        /// </summary>
        public abstract Boolean ReadOnly { get; }

        /// <summary>
        /// Sets this buffer's mark at its position.
        /// </summary>
        public Buffer Mark()
        {
            _mark = _position;
            return this;
        }

        /// <summary>
        /// Resets this buffer's position to the previously-marked position.
        /// </summary>
        public Buffer Reset()
        {
            Int32 m = _mark;
            if (m < 0)
                throw new InvalidOperationException();
            _position = m;
            return this;
        }

        /// <summary>
        /// Clears this buffer.
        /// The position is set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        public Buffer Clear()
        {
            _position = 0;
            _limit = _capacity;
            _mark = -1;
            return this;
        }

        /// <summary>
        /// Flips this buffer.
        /// The limit is set to the current position and then the position is set to zero.
        /// If the mark is defined then it is discarded.
        /// </summary>
        public Buffer Flip()
        {
            _limit = _position;
            _position = 0;
            _mark = -1;
            return this;
        }

        /// <summary>
        /// Rewinds this buffer.
        /// The position is set to zero and the mark is discarded.
        /// </summary>
        public Buffer Rewind()
        {
            _position = 0;
            _mark = -1;
            return this;
        }

        /// <summary>
        /// Gets current mark.
        /// </summary>
        protected Int32 MarkValue
        {
            get { return _mark; }
            set { _mark = value; }
        }

        /// <summary>
        /// Sets capacity.
        /// </summary>
        /// <param name="capacity">the new capacity</param>
        protected void Recapacity(Int32 capacity)
        {
            _capacity = capacity;
        }

        /// <summary>
        /// Checks the given index against the limit.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// the index is not smaller than the limit or is smaller than zero
        /// </exception>
        protected Int32 CheckIndex(Int32 i)
        {
            if ((i < 0) || (i >= _limit))
                throw new IndexOutOfRangeException();
            return i;
        }

        /// <summary>
        /// Checks the given index against the limit.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// the index + number of bytes is not smaller than the limit or is smaller than zero
        /// </exception>
        protected Int32 CheckIndex(Int32 i, Int32 nb)
        {
            if ((i < 0) || (nb > _limit - i))
                throw new IndexOutOfRangeException();
            return i;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="BufferUnderflowException">
        /// the current position is not smaller than the limit
        /// </exception>
        protected Int32 NextGetIndex()
        {
            if (_position >= _limit)
                throw new BufferUnderflowException();
            return _position++;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position with given number of bytes.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="BufferUnderflowException">
        /// the current position is not enough for the given number of bytes
        /// </exception>
        protected Int32 NextGetIndex(Int32 nb)
        {
            if (_limit - _position < nb)
                throw new BufferUnderflowException();
            Int32 p = _position;
            _position += nb;
            return p;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="OverflowException">
        /// the current position is not smaller than the limit
        /// </exception>
        protected Int32 NextPutIndex()
        {
            if (_position >= _limit)
                throw new OverflowException();
            return _position++;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position with given number of bytes.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="OverflowException">
        /// the current position is not enough for the given number of bytes
        /// </exception>
        protected Int32 NextPutIndex(Int32 nb)
        {
            if (_limit - _position < nb)
                throw new OverflowException();
            Int32 p = _position;
            _position += nb;
            return p;
        }

        /// <summary>
        /// Checks the bounds.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        protected static void CheckBounds(Int32 off, Int32 len, Int32 size)
        {
            if ((off | len | (off + len) | (size - (off + len))) < 0)
                throw new IndexOutOfRangeException();
        }
    }

    /// <summary>
    /// Byte order
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Big-endian
        /// </summary>
        BigEndian,
        /// <summary>
        /// Little-endian
        /// </summary>
        LittleEndian
    }
}
