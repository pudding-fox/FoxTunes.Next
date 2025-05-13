using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FoxTunes
{
    public class ImageBrushCache<T>
    {
        public ImageBrushCache(int capacity)
        {
            this.Store = new CappedDictionary<Key, ImageBrush>(capacity);
        }

        public CappedDictionary<Key, ImageBrush> Store { get; private set; }

        public bool TryGetValue(T value, int width, int height, bool preserveAspectRatio, out ImageBrush brush)
        {
            var key = new Key(value, width, height, preserveAspectRatio);
            return this.Store.TryGetValue(key, out brush);
        }

        public ImageBrush GetOrAdd(T value, int width, int height, bool preserveAspectRatio, Func<ImageBrush> factory)
        {
            var key = new Key(value, width, height, preserveAspectRatio);
            return this.Store.GetOrAdd(key, factory);
        }

        public void Clear()
        {
            this.Store.Clear();
        }

        public class Key : IEquatable<Key>
        {
            public Key(T value, int width, int height, bool preserveAspectRatio)
            {
                this.Value = value;
                this.Width = width;
                this.Height = height;
                this.PreserveAspectRatio = preserveAspectRatio;
            }

            public T Value { get; private set; }

            public int Width { get; private set; }

            public int Height { get; private set; }

            public bool PreserveAspectRatio { get; private set; }

            public virtual bool Equals(Key other)
            {
                if (other == null)
                {
                    return false;
                }
                if (object.ReferenceEquals(this, other))
                {
                    return true;
                }
                if (!EqualityComparer<T>.Default.Equals(this.Value, other.Value))
                {
                    return false;
                }
                if (this.Width != other.Width)
                {
                    return false;
                }
                if (this.Height != other.Height)
                {
                    return false;
                }
                if (this.PreserveAspectRatio != other.PreserveAspectRatio)
                {
                    return false;
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Key);
            }

            public override int GetHashCode()
            {
                var hashCode = default(int);
                unchecked
                {
                    if (!EqualityComparer<T>.Default.Equals(this.Value, default(T)))
                    {
                        hashCode += this.Value.GetHashCode();
                    }
                    hashCode += this.Width.GetHashCode();
                    hashCode += this.Height.GetHashCode();
                    hashCode += this.PreserveAspectRatio.GetHashCode();
                }
                return hashCode;
            }

            public static bool operator ==(Key a, Key b)
            {
                if ((object)a == null && (object)b == null)
                {
                    return true;
                }
                if ((object)a == null || (object)b == null)
                {
                    return false;
                }
                if (object.ReferenceEquals((object)a, (object)b))
                {
                    return true;
                }
                return a.Equals(b);
            }

            public static bool operator !=(Key a, Key b)
            {
                return !(a == b);
            }
        }
    }
}
