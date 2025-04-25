using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ImageLoader : StandardComponent, IConfigurableComponent
    {
        public ImageResizer ImageResizer { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public bool HighQualityResizer { get; private set; }

        public Cache Store { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.ImageResizer = ComponentRegistry.Instance.GetComponent<ImageResizer>();
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                ImageBehaviourConfiguration.SECTION,
                ImageLoaderConfiguration.HIGH_QUALITY_RESIZER
            ).ConnectValue(value => this.HighQualityResizer = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                ImageBehaviourConfiguration.SECTION,
                ImageLoaderConfiguration.CACHE_SIZE
            ).ConnectValue(value => this.Store = new Cache(value));
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.ImagesUpdated:
                    this.Store.Clear();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ImageLoaderConfiguration.GetConfigurationSections();
        }

        public ImageSource Load(string fileName, int width, int height, bool preserveAspectRatio, bool cache)
        {
            if (cache)
            {
                return this.Store.GetOrAdd(fileName, width, height, () => this.LoadCore(fileName, width, height, preserveAspectRatio));
            }
            return this.LoadCore(fileName, width, height, preserveAspectRatio);
        }

        private ImageSource LoadCore(string fileName, int width, int height, bool preserveAspectRatio)
        {
            try
            {
                var decode = false;
                if (width != 0 && height != 0 && this.HighQualityResizer)
                {
                    fileName = this.ImageResizer.Resize(fileName, width, height, preserveAspectRatio);
                }
                else
                {
                    decode = true;
                }
                var source = new BitmapImage();
                source.BeginInit();
                source.CacheOption = BitmapCacheOption.OnLoad;
                //TODO: This is much faster than using UriSource.
                //TODO: See: https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/Media/Imaging/BitmapDecoder.cs,cd1c67c55a73f984
                using (source.StreamSource = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (decode)
                    {
                        if (width != 0)
                        {
                            source.DecodePixelWidth = width;
                        }
                        else if (height != 0)
                        {
                            source.DecodePixelHeight = height;
                        }
                    }
                    source.EndInit();
                    if (source.CanFreeze)
                    {
                        source.Freeze();
                    }
                }
                return source;
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ImageLoader), LogLevel.Warn, "Failed to load image: {0}", e.Message);
                return null;
            }
        }

        public ImageSource Load(string id, Func<Stream> factory, int width, int height, bool cache)
        {
            if (cache)
            {
                return this.Store.GetOrAdd(id, width, height, () => this.LoadCore(factory, width, height));
            }
            return this.LoadCore(factory, width, height);
        }

        public ImageSource LoadCore(Func<Stream> factory, int width, int height)
        {
            try
            {
                var source = new BitmapImage();
                source.BeginInit();
                source.CacheOption = BitmapCacheOption.OnLoad;
                using (var stream = factory())
                {
                    source.StreamSource = stream;
                    if (width != 0)
                    {
                        source.DecodePixelWidth = width;
                    }
                    else if (height != 0)
                    {
                        source.DecodePixelHeight = height;
                    }
                    source.EndInit();
                    if (source.CanFreeze)
                    {
                        source.Freeze();
                    }
                }
                return source;
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ImageLoader), LogLevel.Warn, "Failed to load image: {0}", e.Message);
                return null;
            }
        }

        public class Cache
        {
            public Cache(int capacity)
            {
                this.Store = new CappedDictionary<Key, ImageSource>(capacity);
            }

            public CappedDictionary<Key, ImageSource> Store { get; private set; }

            public ImageSource GetOrAdd(string fileName, int width, int height, Func<ImageSource> factory)
            {
                var key = new Key(fileName, width, height);
                return this.Store.GetOrAdd(key, factory);
            }

            public void Clear()
            {
                this.Store.Clear();
            }

            public class Key : IEquatable<Key>
            {
                public Key(string fileName, int width, int height)
                {
                    this.FileName = fileName;
                    this.Width = width;
                    this.Height = height;
                }

                public string FileName { get; private set; }

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
                    if (!string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase))
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
                        if (!string.IsNullOrEmpty(this.FileName))
                        {
                            hashCode += this.FileName.ToLower().GetHashCode();
                        }
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
