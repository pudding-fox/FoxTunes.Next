using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FoxTunes
{
    public class CappedDictionary<TKey, TValue>
    {
        public readonly object SyncRoot = new object();

        public CappedDictionary(int capacity)
        {
            this.Keys = new Queue(capacity);
            this.Store = new ConcurrentDictionary<TKey, Lazy<TValue>>(Environment.ProcessorCount, capacity);
            this.Capacity = capacity;
        }

        public CappedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.Keys = new Queue(capacity);
            this.Store = new ConcurrentDictionary<TKey, Lazy<TValue>>(Environment.ProcessorCount, capacity, comparer);
            this.Capacity = capacity;
        }

        public Queue Keys { get; private set; }

        public ConcurrentDictionary<TKey, Lazy<TValue>> Store { get; private set; }

        public int Capacity { get; private set; }

        [Obsolete("Please use the concurrent method: GetOrAdd(key, value)")]
        public void Add(TKey key, TValue value)
        {
            this.Store.AddOrUpdate(key, new Lazy<TValue>(() => value));
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            return this.Store.GetOrAdd(key, new Lazy<TValue>(() =>
            {
                this.OnAdd(key, value);
                return value;
            })).Value;
        }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            return this.Store.GetOrAdd(key, new Lazy<TValue>(() =>
            {
                var value = factory();
                this.OnAdd(key, value);
                return value;
            })).Value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var lazy = default(Lazy<TValue>);
            var result = this.Store.TryGetValue(key, out lazy);
            if (result)
            {
                lock (this.SyncRoot)
                {
                    //When a cache hit occurs, push the key to the back of queue.
                    this.Keys.Remove(key);
                    this.Keys.Enqueue(key);
                }
                value = lazy.Value;
            }
            else
            {
                value = default(TValue);
            }
            return result;
        }

        public bool TryRemove(TKey key)
        {
            var value = default(TValue);
            return this.TryRemove(key, out value);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            var lazy = default(Lazy<TValue>);
            var result = this.Store.TryRemove(key, out lazy);
            if (result)
            {
                this.OnRemove(key, lazy.Value);
                value = lazy.Value;
            }
            else
            {
                value = default(TValue);
            }
            return result;
        }

        protected virtual void OnAdd(TKey key, TValue value)
        {
            lock (this.SyncRoot)
            {
                while (this.Keys.Count >= (this.Capacity - 1))
                {
                    this.Store.TryRemove(this.Keys.Dequeue());
                }
                this.Keys.Enqueue(key);
            }
        }

        protected virtual void OnRemove(TKey key, TValue value)
        {
            lock (this.SyncRoot)
            {
                this.Keys.Remove(key);
            }
        }

        public void Clear()
        {
            this.Store.Clear();
            lock (this.SyncRoot)
            {
                this.Keys.Clear();
            }
        }

        public class Queue
        {
            public Queue(int capacity)
            {
                this.Values = new List<TKey>(capacity);
            }

            public List<TKey> Values { get; private set; }

            public void Enqueue(TKey value)
            {
                this.Values.Add(value);
            }

            public void Remove(TKey value)
            {
                this.Values.Remove(value);
            }

            public TKey Dequeue()
            {
                if (this.Values.Count > 0)
                {
                    var value = this.Values[0];
                    this.Values.RemoveAt(0);
                    return value;
                }
                return default(TKey);
            }

            public void Clear()
            {
                this.Values.Clear();
            }

            public int Count
            {
                get
                {
                    return this.Values.Count;
                }
            }
        }
    }
}
