using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class BassLoader : StandardComponent, IBassLoader
    {
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

        public static readonly HashSet<string> PATHS = new HashSet<string>(new[]
        {
            Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "Addon")
        }, StringComparer.OrdinalIgnoreCase);

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

        public static bool AddPath(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                return PATHS.Add(path);
            }
            return false;
        }

        static BassLoader()
        {
            Loader.Load("bass.dll");
            Loader.Load("bass_fx.dll");
            Loader.Load("bassmix.dll");
            FxVersion = BassFx.Version;
            MixVersion = BassMix.Version;
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
            return this.Extensions.Contains(extension);
        }

        public void Load()
        {
            if (this.IsLoaded)
            {
                return;
            }
            var failures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in PATHS)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        if (this.Load(path))
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to load plugin \"{0}\": {1}", path, e.Message);
                    }
                    failures.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    foreach (var fileName in Directory.EnumerateFiles(path, FILE_NAME_MASK))
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
            var handle = Bass.PluginLoad(fileName);
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
}
