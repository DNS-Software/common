using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DNS.Common.Concurrency
{
    /// <summary>
    /// Priority queue that does guarantee FIFO,
    /// this does mean the Enqueue operation is O(n log n) operation in comparison with
    /// .NET 6 and higher PriorityQueue that has a 0 (log n) Enqueue and Dequeue operations.
    /// </summary>
    public sealed class PriorityQueue<TKey, TValue>
    {
        private readonly object _lock = new object();
        private readonly List<Element<TValue>> _collection;
        private readonly IList<TKey> _prioritisedKeys;

        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return !_collection.Any();
                }
            }
        }

        public event Action ValueEnqueued;

        public PriorityQueue()
        {
            _collection = new List<Element<TValue>>();
            _prioritisedKeys = Array.Empty<TKey>();
        }
        
        public PriorityQueue(IList<TKey> prioritisedKeys) : this()
        {
            _prioritisedKeys = prioritisedKeys;
        }

        public void Enqueue(TKey key, TValue value)
        {
            lock (_lock)
            {
                _collection.Add(new Element<TValue>(GetPriority(), value));
                
                if (_prioritisedKeys.Any())
                {
                    _collection.Sort();
                }
            }

            Task.Run(() => ValueEnqueued?.Invoke());
            
            int GetPriority()
            {
                var index = _prioritisedKeys.IndexOf(key);
                return index != -1 ? index : int.MaxValue;
            }
        }

        public TValue Dequeue()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The queue is empty");
            }
            
            lock (_lock)
            {
                var nextElement = _collection[0];
                _collection.RemoveAt(0);
                return nextElement.Value;
            }
        }
        
        private sealed class Element<T> : IComparable<Element<T>>
        {
            private readonly int _priority;
            public T Value { get; }

            public Element(int priority, T value)
            {
                _priority = priority;
                Value = value;
            }

            public int CompareTo(Element<T> other)
            {
                if (_priority > other._priority)
                {
                    return 1;
                }

                if (_priority < other._priority)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}