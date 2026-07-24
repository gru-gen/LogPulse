using System.Collections;

namespace LogPulse.Domain.Collections;

/// <summary>
/// A fixed-size buffer that keeps the newest N items. When full, each Add
/// overwrites the oldest item. Enumeration and the indexer work in AGE order:
/// position 0 = oldest, Count-1 = newest — like reading a log top to bottom.
/// </summary>
public class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _items;
    private int _head;
    private int _count;
    private int _version;

    /// <summary>
    /// Creates an empty ring holding at most <paramref name="capacity"/> items.
    /// </summary>
    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new T[capacity];
    }

    /// <summary>
    /// How many items the ring holds right now.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// How many items the ring can hold (fixed at construction).
    /// </summary>
    public int Capacity => _items.Length;

    /// <summary>
    /// The item at a position in age order: 0 = oldest, Count-1 = newest.
    /// </summary>
    public T this[int position]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(position);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, _count);
            return _items[(OldestItemPosition + position) % _items.Length];
        }
    }

    /// <summary>
    /// Index-from-end support, so ring[^1] means "the newest item".
    /// </summary>
    public T this[Index index] => this[index.GetOffset(_count)];

    /// <summary>
    /// The newest item. Throws when the ring is empty.
    /// </summary>
    public T Latest =>
        _count > 0 ? this[^1] : throw new InvalidOperationException("Buffer is empty.");

    /// <summary>
    /// Adds one item, overwriting the oldest when the ring is full.
    /// </summary>
    public void Add(T item)
    {
        _items[_head] = item;
        _head = (_head + 1) % _items.Length;
        if (_count < _items.Length)
            _count++;

        _version++;
    }

    private int OldestItemPosition => _count < _items.Length ? 0 : _head;

    /// <summary>
    /// Returns the value-type enumerator. Items come back oldest first.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// The enumerator for this ring.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly RingBuffer<T> _buffer;
        private readonly int _version;
        private int _position = -1;

        internal Enumerator(RingBuffer<T> buffer)
        {
            _buffer = buffer;
            _version = buffer._version;
        }

        /// <summary>
        /// The item that the last MoveNext call stopped on.
        /// </summary>
        public readonly T Current => _buffer[_position];

        readonly object? IEnumerator.Current => Current;

        /// <summary>
        /// Advances to the next item. Returns false after the last one. Throws if the ring changed.
        /// </summary>
        public bool MoveNext()
        {
            if (_version != _buffer._version)
                throw new InvalidOperationException("RingBuffer was modified during enumeration.");

            _position++;
            return _position < _buffer.Count;
        }

        /// <summary>
        /// Does nothing. The enumeration protocol requires the method to exist.
        /// </summary>
        public readonly void Dispose() { }
      
        /// <summary>
        /// Not supported. Reset is a legacy member of the old enumerator interface.
        /// </summary>
        public void Reset() => throw new NotSupportedException();
    }
}
