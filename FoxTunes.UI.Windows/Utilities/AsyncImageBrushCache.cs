using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FoxTunes
{
    public class AsyncImageBrushCache<T>
    {
        public AsyncImageBrushCache(int capacity)
        {
            this.Store = new CappedDictionary<ImageBrushCache<T>.Key, Task<ImageBrush>>(capacity);
        }

        public CappedDictionary<ImageBrushCache<T>.Key, Task<ImageBrush>> Store { get; private set; }

        public bool TryGetValue(T value, int width, int height, bool preserveAspectRatio, out Task<ImageBrush> brush)
        {
            var key = new ImageBrushCache<T>.Key(value, width, height, preserveAspectRatio);
            return this.Store.TryGetValue(key, out brush);
        }

        public Task<ImageBrush> GetOrAdd(T value, int width, int height, bool preserveAspectRatio, Func<Task<ImageBrush>> factory)
        {
            var key = new ImageBrushCache<T>.Key(value, width, height, preserveAspectRatio);
            return this.Store.GetOrAdd(key, factory);
        }

        public bool TryRemove(T value)
        {
            var result = default(bool);
            foreach (var pair in this.Store)
            {
                if (object.Equals(pair.Key.Value, value))
                {
                    result |= this.Store.TryRemove(pair.Key);
                }
            }
            return result;
        }

        public void Clear()
        {
            this.Store.Clear();
        }
    }
}
