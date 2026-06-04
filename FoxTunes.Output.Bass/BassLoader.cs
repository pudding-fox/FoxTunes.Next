using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public class BassLoader : StandardComponent, IBassLoader
    {
        public const byte PRIORITY_HIGH = 0;

        public const byte PRIORITY_NORMAL = 100;

        public const byte PRIORITY_LOW = 255;

        public const string DIRECTORY_NAME_ADDON = "Addon";

        public const string FILE_NAME_MASK = "bass*.dll";

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassLoader).Assembly.Location);
            }
        }

        public static readonly HashSet<string> EXTENSIONS = new HashSet<string>(new[]
        {
            "mp1", "mp2", "mp3", "ogg", "wav", "aif"
        }, StringComparer.OrdinalIgnoreCase);

        public static readonly HashSet<string> BLACKLIST = new HashSet<string>(new[]
        {
            "bin",
            "txt",
            "nfo"
        }, StringComparer.OrdinalIgnoreCase);

        public static readonly HashSet<BassLoaderPath> PATHS = new HashSet<BassLoaderPath>(new[]
        {
            new BassLoaderPath( Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "Addon"))
        }, BassLoaderPathComparer.Instance);

        public static object SyncRoot = new object();

        public static readonly Version FxVersion;

        public static readonly Version MixVersion;

        public static void AddExtensions(IEnumerable<string> extensions)
        {
            foreach (var extension in extensions)
            {
                AddExtension(extension);
            }
        }

        public static void AddExtension(string extension)
        {
            EXTENSIONS.Add(extension);
        }

        public static bool AddPath(string path, byte priority = BassLoader.PRIORITY_NORMAL)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                return PATHS.Add(new BassLoaderPath(path, priority));
            }
            return false;
        }

        static BassLoader()
        {
            try
            {
                Loader.Load("bass.dll");
                Loader.Load("bass_fx.dll");
                Loader.Load("bassmix.dll");
                FxVersion = BassFx.Version;
                MixVersion = BassMix.Version;
            }
            catch (Exception)
            {
                Logger.Write(typeof(BassLoader), LogLevel.Error, "Failed to load bass.dll, bass_fx.dll or bass_mix.dll.");
            }
        }

        public BassLoader()
        {
            this._Extensions = new Lazy<IEnumerable<string>>(() =>
            {
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var extension in EXTENSIONS)
                {
                    extensions.Add(extension);
                }
                foreach (var plugin in this.Plugins)
                {
                    foreach (var format in plugin.Info.Formats)
                    {
                        if (format.FileExtensions == null)
                        {
                            continue;
                        }
                        foreach (var extension in format.FileExtensions.Split(';'))
                        {
                            extensions.Add(extension.TrimStart('*', '.'));
                        }
                    }
                }
                return extensions;
            });
            this.Plugins = new HashSet<BassPlugin>();
        }

        public Lazy<IEnumerable<string>> _Extensions { get; private set; }

        public IEnumerable<string> Extensions
        {
            get
            {
                return this._Extensions.Value;
            }
        }

        public HashSet<BassPlugin> Plugins { get; private set; }

        private bool _IsLoaded { get; set; }

        public bool IsLoaded
        {
            get
            {
                return this._IsLoaded;
            }
            private set
            {
                this._IsLoaded = value;
                this.OnIsLoadedChanged();
            }
        }

        protected virtual void OnIsLoadedChanged()
        {
            if (this.IsLoadedChanged != null)
            {
                this.IsLoadedChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsLoaded");
        }

        public event EventHandler IsLoadedChanged;

        public override void InitializeComponent(ICore core)
        {
            this.Load();
            base.InitializeComponent(core);
        }

        public bool IsSupported(string extension)
        {
            return this.Extensions.Contains(extension) && !BLACKLIST.Contains(extension);
        }

        public void Load()
        {
            if (this.IsLoaded)
            {
                return;
            }
            var failures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in PATHS.OrderBy(_path => _path.Priority).ThenBy(_path => _path.Path.ToLower()/*Maintain compatibility.*/))
            {
                if (File.Exists(path.Path))
                {
                    try
                    {
                        if (this.Load(path.Path))
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to load plugin \"{0}\": {1}", path, e.Message);
                    }
                    failures.Add(path.Path);
                }
                else if (Directory.Exists(path.Path))
                {
                    foreach (var fileName in Directory.EnumerateFiles(path.Path, FILE_NAME_MASK))
                    {
                        try
                        {
                            if (this.Load(fileName))
                            {
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Write(this, LogLevel.Warn, "Failed to load plugin \"{0}\": {1}", fileName, e.Message);
                        }
                        failures.Add(fileName);
                    }
                }
            }
            //We don't have anything to handle plugin inter-dependency, hopefully the second attempt will work.
            if (failures.Any())
            {
                Logger.Write(this, LogLevel.Warn, "At least one plugin failed to load, retrying..");
                foreach (var fileName in failures)
                {
                    try
                    {
                        this.Load(fileName);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to load plugin \"{0}\": {1}", fileName, e.Message);
                    }
                }
            }
            this.IsLoaded = true;
        }

        public bool Load(string fileName)
        {
            var handle = default(int);
            var directoryName = Path.GetDirectoryName(fileName);
            this.WithDllDirectory(directoryName, () => handle = Bass.PluginLoad(fileName));
            if (handle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load plugin: {0}", fileName);
                return false;
            }
            var info = Bass.PluginGetInfo(handle);
            Logger.Write(this, LogLevel.Debug, "Plugin loaded \"{0}\": {1}", fileName, info.Version);
            this.Plugins.Add(new BassPlugin(
                fileName,
                info,
                handle
            ));
            return true;
        }

        public void Unload()
        {
            foreach (var plugin in this.Plugins)
            {
                Bass.PluginFree(plugin.Handle);
            }
            this.Plugins.Clear();
            Loader.Free("bassmix.dll");
            Loader.Free("bass_fx.dll");
            Loader.Free("bass.dll");
            this.IsLoaded = false;
        }

        protected virtual void WithDllDirectory(string directoryName, Action action)
        {
            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_USER_DIRS);
            var cookie = AddDllDirectory(directoryName);
            try
            {
                action();
            }
            finally
            {
                if (!IntPtr.Zero.Equals(cookie))
                {
                    RemoveDllDirectory(cookie);
                }
            }
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
            this.Unload();
        }

        ~BassLoader()
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

        public const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        public const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

        [DllImport("kernel32.dll")]
        public static extern bool SetDefaultDllDirectories(uint DirectoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr AddDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool RemoveDllDirectory(IntPtr Cookie);

        public class BassPlugin : IEquatable<BassPlugin>
        {
            public BassPlugin(string fileName, PluginInfo info, int handle)
            {
                this.FileName = fileName;
                this.Info = info;
                this.Handle = handle;
            }

            public string FileName { get; private set; }

            public PluginInfo Info { get; private set; }

            public int Handle { get; private set; }

            public override int GetHashCode()
            {
                var hashCode = default(int);
                if (!string.IsNullOrEmpty(this.FileName))
                {
                    hashCode += this.FileName.ToLower().GetHashCode();
                }
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as BassPlugin);
            }

            public bool Equals(BassPlugin other)
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
                return true;
            }

            public static bool operator ==(BassPlugin a, BassPlugin b)
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

            public static bool operator !=(BassPlugin a, BassPlugin b)
            {
                return !(a == b);
            }
        }
    }

    public class BassLoaderPath
    {
        public BassLoaderPath(string path, byte priority = BassLoader.PRIORITY_NORMAL)
        {
            this.Path = path;
            this.Priority = priority;
        }

        public string Path { get; private set; }

        public byte Priority { get; private set; }
    }

    public class BassLoaderPathComparer : IEqualityComparer<BassLoaderPath>
    {
        public bool Equals(BassLoaderPath x, BassLoaderPath y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (!string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public int GetHashCode(BassLoaderPath obj)
        {
            var hashCode = default(int);
            unchecked
            {
                hashCode += obj.Path.ToLower().GetHashCode();
            }
            return hashCode;
        }

        public static readonly BassLoaderPathComparer Instance = new BassLoaderPathComparer();

    }
}
