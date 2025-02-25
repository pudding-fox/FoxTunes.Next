using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public abstract class IndexedCollection
    {
        public enum IndexType : byte
        {
            None = 0,
            Single = 1,
            Multiple = 2
        }
    }

    public abstract class IndexedCollection<T> : IndexedCollection
    {
        public IndexedCollection()
        {
            this.Indexes = new ConcurrentDictionary<object, Index>();
        }

        public ConcurrentDictionary<object, Index> Indexes { get; private set; }

        public abstract ICollection<T> InnerCollection { get; }

        public Index<TKey> By<TKey>(Func<T, TKey> selector, IndexType indexType)
        {
            switch (indexType)
            {
                case IndexType.Single:
                    return this.Indexes.GetOrAdd(
                        selector,
                        key => new SingleRecordIndex<TKey>(this, selector)
                    ) as Index<TKey>;
                case IndexType.Multiple:
                    return this.Indexes.GetOrAdd(
                        selector,
                        key => new MultipleRecordIndex<TKey>(this, selector)
                    ) as Index<TKey>;
                default:
                    throw new NotImplementedException();
            }
        }

        public void WithIndexes(Action<Index> action)
        {
            var indexes = this.Indexes.Values.ToArray();
            foreach (var index in indexes)
            {
                action(index);
            }
        }

        protected virtual void OnAdded(T item)
        {
            this.WithIndexes(index => index.TryAdd(item));
        }

        protected virtual void OnRemoved(T item)
        {
            this.WithIndexes(index => index.TryRemove(item));
        }

        protected virtual void OnCleared()
        {
            var keys = this.Indexes.Keys.ToArray();
            this.WithIndexes(index => index.Clear());
        }

        public abstract class Index
        {
            public abstract bool TryAdd(T item);

            public abstract bool TryRemove(T item);

            public abstract void Clear();
        }

        public abstract class Index<TKey> : Index
        {
            protected Index(Func<T, TKey> selector)
            {
                this.Selector = selector;
            }

            public Func<T, TKey> Selector { get; private set; }

            public abstract bool Contains(TKey key);

            public abstract T Find(TKey key);

            public abstract bool TryFind(TKey key, out T value);

            public abstract T[] FindAll(TKey key);
        }

        public class SingleRecordIndex<TKey> : Index<TKey>
        {
            public SingleRecordIndex(IndexedCollection<T> sequence, Func<T, TKey> selector)
                : base(selector)
            {
                this.InnerDictionary = new ConcurrentDictionary<TKey, T>();
                foreach (var element in sequence.InnerCollection)
                {
                    this.TryAdd(element);
                }
            }

            public ConcurrentDictionary<TKey, T> InnerDictionary { get; private set; }

            public override bool Contains(TKey key)
            {
                return this.InnerDictionary.ContainsKey(key);
            }

            public override bool TryAdd(T item)
            {
                var key = this.Selector(item);
                if (!this.InnerDictionary.TryAdd(key, item))
                {
                    return false;
                }
                return true;
            }

            public override bool TryRemove(T item)
            {
                var key = this.Selector(item);
                if (!this.InnerDictionary.TryRemove(key, out item))
                {
                    return false;
                }
                return true;
            }

            public override void Clear()
            {
                this.InnerDictionary.Clear();
            }

            public override T Find(TKey key)
            {
                var value = default(T);
                if (!this.InnerDictionary.TryGetValue(key, out value))
                {
                    return default(T);
                }
                return value;
            }

            public override bool TryFind(TKey key, out T value)
            {
                return this.InnerDictionary.TryGetValue(key, out value);
            }

            public override T[] FindAll(TKey key)
            {
                var item = this.Find(key);
                if (EqualityComparer<T>.Default.Equals(default(T), item))
                {
                    return new T[] { };
                }
                return new[] { item };
            }

        }

        public class MultipleRecordIndex<TKey> : Index<TKey>
        {
            public MultipleRecordIndex(IndexedCollection<T> sequence, Func<T, TKey> selector)
                : base(selector)
            {
                this.InnerDictionary = new ConcurrentDictionary<TKey, IList<T>>();
                foreach (var element in sequence.InnerCollection)
                {
                    this.TryAdd(element);
                }
            }

            public ConcurrentDictionary<TKey, IList<T>> InnerDictionary { get; private set; }

            public override bool Contains(TKey key)
            {
                return this.InnerDictionary.ContainsKey(key);
            }

            public override bool TryAdd(T item)
            {
                var key = this.Selector(item);
                this.InnerDictionary.AddOrUpdate(
                    key,
                    _key => new List<T>(new[] { item }),
                    (_key, list) =>
                    {
                        list.Add(item);
                        return list;
                    }
                );
                return true;
            }

            public override bool TryRemove(T item)
            {
                var key = this.Selector(item);
                var list = default(IList<T>);
                if (!this.InnerDictionary.TryGetValue(key, out list))
                {
                    return false;
                }
                var success = list.Remove(item);
                if (list.Count == 0)
                {
                    this.InnerDictionary.TryRemove(key, out list);
                }
                return success;
            }

            public override void Clear()
            {
                this.InnerDictionary.Clear();
            }

            public override T Find(TKey key)
            {
                var list = default(IList<T>);
                if (!this.InnerDictionary.TryGetValue(key, out list))
                {
                    return default(T);
                }
                return list.FirstOrDefault();
            }

            public override bool TryFind(TKey key, out T value)
            {
                var list = default(IList<T>);
                if (!this.InnerDictionary.TryGetValue(key, out list))
                {
                    value = default(T);
                    return false;
                }
                value = list.FirstOrDefault();
                if (EqualityComparer<T>.Default.Equals(default(T), value))
                {
                    value = default(T);
                    return false;
                }
                return true;
            }

            public override T[] FindAll(TKey key)
            {
                var list = default(IList<T>);
                if (!this.InnerDictionary.TryGetValue(key, out list))
                {
                    return new T[] { };
                }
                return list.ToArray();
            }
        }
    }
}