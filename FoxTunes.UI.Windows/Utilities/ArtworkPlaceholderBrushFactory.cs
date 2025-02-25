using FoxTunes.Interfaces;
using System;
using System.Windows.Media;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ArtworkPlaceholderBrushFactory : StandardFactory, IDisposable
    {
        const int CACHE_SIZE = 32;

        public ArtworkPlaceholderBrushFactory()
        {
            this.Store = new Cache(CACHE_SIZE);
        }

        public Cache Store { get; private set; }

        public PixelSizeConverter PixelSizeConverter { get; private set; }

        public ImageLoader ImageLoader { get; private set; }

        public ThemeLoader ThemeLoader { get; private set; }

        public ImageBrush Create(int width, int height)
        {
            this.PixelSizeConverter.Convert(ref width, ref height);
            return this.Store.GetOrAdd(width, height, () => this.Create(width, height, true));
        }

        protected virtual ImageBrush Create(int width, int height, bool cache)
        {
            Logger.Write(this, LogLevel.Debug, "Creating brush: {0}x{1}", width, height);
            var source = ImageLoader.Load(
               this.ThemeLoader.Theme.Id,
               this.ThemeLoader.Theme.GetArtworkPlaceholder,
               width,
               height,
               cache
           );
            if (source == null)
            {
                return null;
            }
            var brush = new ImageBrush(source)
            {
                Stretch = Stretch.Uniform
            };
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }
            return brush;
        }

        public override void InitializeComponent(ICore core)
        {
            this.PixelSizeConverter = ComponentRegistry.Instance.GetComponent<PixelSizeConverter>();
            this.ImageLoader = ComponentRegistry.Instance.GetComponent<ImageLoader>();
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            this.ThemeLoader.ThemeChanged += this.OnThemeChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnThemeChanged(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Theme was changed, resetting cache.");
            this.Store.Clear();
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.ThemeLoader != null)
            {
                this.ThemeLoader.ThemeChanged -= this.OnThemeChanged;
            }
        }

        ~ArtworkPlaceholderBrushFactory()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public class Cache
        {
            public Cache(int capacity)
            {
                this.Store = new CappedDictionary<Key, ImageBrush>(capacity);
            }

            public CappedDictionary<Key, ImageBrush> Store { get; private set; }

            public ImageBrush GetOrAdd(int width, int height, Func<ImageBrush> factory)
            {
                var key = new Key(width, height);
                return this.Store.GetOrAdd(key, factory);
            }

            public void Clear()
            {
                this.Store.Clear();
            }

            public class Key : IEquatable<Key>
            {
                public Key(int width, int height)
                {
                    this.Width = width;
                    this.Height = height;
                }

                public int Width { get; private set; }

                public int Height { get; private set; }

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
                    if (this.Width != other.Width)
                    {
                        return false;
                    }
                    if (this.Height != other.Height)
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
                        hashCode += this.Width.GetHashCode();
                        hashCode += this.Height.GetHashCode();
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
}
