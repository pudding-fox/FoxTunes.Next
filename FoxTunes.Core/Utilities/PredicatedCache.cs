using System;

namespace FoxTunes
{
    public abstract class PredicatedCache<TKey, TValue>
    {
        protected PredicatedCache(int capacity)
        {
            this.Store = new CappedDictionary<TKey, TValue>(capacity);
        }

        public CappedDictionary<TKey, TValue> Store { get; private set; }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            if (!this.CanCache(key))
            {
                return factory();
            }
            return this.Store.GetOrAdd(key, factory);
        }

        protected abstract bool CanCache(TKey key);
    }
}
