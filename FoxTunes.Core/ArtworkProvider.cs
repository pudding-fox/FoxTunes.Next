using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ArtworkProvider : StandardComponent, IArtworkProvider
    {
        const int CACHE_SIZE = 5120;

        const string DELIMITER = ",";

        public static readonly string[] EXTENSIONS = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".bin" };

        public ArtworkProvider()
        {
            this.Store = new Cache(CACHE_SIZE);
        }

        public Cache Store { get; private set; }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public bool Enabled { get; private set; }

        public string[] Front { get; private set; }

        public string[] Back { get; private set; }

        public string[] Folders { get; private set; }

        public int MaxSize { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.OnDemandMetaDataProvider = core.Components.OnDemandMetaDataProvider;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_LOOSE_IMAGES
            ).ConnectValue(value => this.Enabled = value);
            this.Configuration.GetElement<TextConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.LOOSE_IMAGES_FRONT
           ).ConnectValue(value =>
           {
               this.Front = this.Parse(value);
               this.Store.Clear();
           });
            this.Configuration.GetElement<TextConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.LOOSE_IMAGES_BACK
           ).ConnectValue(value =>
           {
               this.Back = this.Parse(value);
               this.Store.Clear();
           });
            this.Configuration.GetElement<TextConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.LOOSE_IMAGES_FOLDER
            ).ConnectValue(value =>
            {
                this.Folders = this.Parse(value);
                this.Store.Clear();
            });
            this.Configuration.GetElement<IntegerConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.MAX_IMAGE_SIZE
            ).ConnectValue(value =>
            {
                this.MaxSize = value * 1024000;
                this.Store.Clear();
            });
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

        protected virtual string[] Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new string[] { };
            }
            return value
                .Split(new[] { DELIMITER }, StringSplitOptions.RemoveEmptyEntries)
                .Select(element => element.Trim())
                .ToArray();
        }

        public string GetFileName(string path, string extension, ArtworkType type)
        {
            var absolutePath = default(string);
            if (!path.GetAbsolutePath(out absolutePath))
            {
                return string.Empty;
            }
            var directoryName = Path.GetDirectoryName(absolutePath);
            var names = default(string[]);
            switch (type)
            {
                case ArtworkType.FrontCover:
                    names = this.Front;
                    break;
                case ArtworkType.BackCover:
                    names = this.Back;
                    break;
                default:
                    throw new NotImplementedException();
            }
            var name = names.FirstOrDefault();
            if (string.IsNullOrEmpty(name))
            {
                name = "cover";
            }
            return Path.Combine(directoryName, string.Concat(name, ".", extension.TrimStart('.')));
        }

        public IEnumerable<string> GetDirectoryNames(string root)
        {
            yield return root;
            foreach (var path in this.Folders)
            {
                var folder = Path.Combine(root, path);
                if (!Directory.Exists(folder))
                {
                    continue;
                }
                yield return folder;
            }
        }

        public string Find(string path, ArtworkType type)
        {
            var absolutePath = default(string);
            if (!path.GetAbsolutePath(out absolutePath))
            {
                return string.Empty;
            }
            var root = Path.GetDirectoryName(absolutePath);
            return this.Store.GetOrAdd(root, type, () =>
            {
                var names = default(string[]);
                switch (type)
                {
                    case ArtworkType.FrontCover:
                        names = this.Front;
                        break;
                    case ArtworkType.BackCover:
                        names = this.Back;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                try
                {
                    foreach (var name in names)
                    {
                        foreach (var directoryName in this.GetDirectoryNames(root))
                        {
                            foreach (var fileName in FileSystemHelper.EnumerateFiles(directoryName, string.Format("{0}.*", name), FileSystemHelper.SearchOption.None))
                            {
                                var info = new FileInfo(fileName);
                                if (!EXTENSIONS.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                if (info.Length <= this.MaxSize)
                                {
                                    return fileName;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Error locating artwork of type {0} in {1}: {2}", Enum.GetName(typeof(ArtworkType), type), path, e.Message);
                }
                return null;
            });
        }

        public async Task<string> Find(IFileData fileData, ArtworkType type)
        {
            var fileName = await this.OnDemandMetaDataProvider.GetMetaData(
                fileData,
                new OnDemandMetaDataRequest(
                    Enum.GetName(typeof(ArtworkType), type),
                    MetaDataItemType.Image,
                    MetaDataUpdateType.System
                )
            ).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                return fileName;
            }
            return this.Find(fileData.FileName, type);
        }

        public void Reset(string path, ArtworkType type)
        {
            var absolutePath = default(string);
            if (!path.GetAbsolutePath(out absolutePath))
            {
                return;
            }
            var directoryName = Path.GetDirectoryName(absolutePath);
            this.Store.TryRemove(directoryName, type);
        }

        public class Cache
        {
            public Cache(int capacity)
            {
                this.Store = new CappedDictionary<Key, string>(capacity);
            }

            public CappedDictionary<Key, string> Store { get; private set; }

            public string GetOrAdd(string path, ArtworkType type, Func<string> factory)
            {
                var key = new Key(path, type);
                return this.Store.GetOrAdd(key, factory);
            }

            public bool TryRemove(string path, ArtworkType type)
            {
                var key = new Key(path, type);
                return this.Store.TryRemove(key);
            }

            public void Clear()
            {
                this.Store.Clear();
            }

            public class Key : IEquatable<Key>
            {
                public Key(string path, ArtworkType type)
                {
                    this.Path = path;
                    this.Type = type;
                }

                public string Path { get; private set; }

                public ArtworkType Type { get; private set; }

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
                    if (!string.Equals(this.Path, other.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    if (this.Type != other.Type)
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
                        if (!string.IsNullOrEmpty(this.Path))
                        {
                            hashCode += this.Path.ToLower().GetHashCode();
                        }
                        hashCode += this.Type.GetHashCode();
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
