using System;
using System.Linq;
using System.Threading;

namespace DNS.Common.Concurrency
{
    /// <summary>
    /// Thread safe value wrapper
    /// </summary>
    public sealed class Atomic<T> : IDisposable
    {
        private readonly object _valueLock = new object();
        private readonly ManualResetEvent _valueChangedEvent = new ManualResetEvent(false);
        
        private T _value;
        public T Value
        {
            get
            {
                lock (_valueLock)
                {
                    return _value;
                }
            }

            set
            {
                lock (_valueLock)
                {
                    _value = value;
                }

                _valueChangedEvent.Set();
            }
        }

        public Atomic()
        {
            var valueType = typeof(T);

            if (valueType.IsClass &&
                valueType.GenericTypeArguments.Any() &&
                valueType == typeof(Atomic<>).MakeGenericType(valueType.GenericTypeArguments))
            {
                throw new ArgumentException("Genric type T cannot be of type Atomic<>");
            }
        }

        public Atomic(T initialValue) : this()
        {
            Value = initialValue;
        }
        
        public void WaitForValue(T value)
        {
            while (!Value.Equals(value))
            {
                _valueChangedEvent.WaitOne();
            }
        }

        public void Dispose()
        {
            _valueChangedEvent?.Dispose();
        }
    }
}

