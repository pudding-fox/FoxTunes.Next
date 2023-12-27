using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxTunes
{
    public static class PLSHelper
    {
        public static readonly string[] EXTENSIONS = new[] { ".pls" };

        public const string FILE = "File";

        public const string LENGTH = "Length";

        public class Reader
        {
            public static readonly Regex ENTRY = new Regex(@"^\s*([a-z]+)(\d+?)=(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public Reader(IEnumerable<string> content)
            {
                this.Content = content;
            }

            public IEnumerable<string> Content { get; private set; }

            public IEnumerable<PlaylistItem> Read()
            {
                var content = new Dictionary<int, IDictionary<string, string>>();
                foreach (var line in this.Content)
                {
                    var match = ENTRY.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }
                    var name = match.Groups[1].Value;
                    var index = default(int);
                    if (!int.TryParse(match.Groups[2].Value, out index))
                    {
                        continue;
                    }
                    var value = match.Groups[3].Value;
                    content.GetOrAdd(
                        index,
                        () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    ).TryAdd(name, value);
                }
                return this.Read(content);
            }

            protected virtual IEnumerable<PlaylistItem> Read(IDictionary<int, IDictionary<string, string>> content)
            {
                var playlistItems = new List<PlaylistItem>();
                foreach (var pair in content.OrderBy(pair => pair.Key))
                {
                    var playlistItem = this.Read(pair.Key, pair.Value);
                    if (playlistItem == null)
                    {
                        continue;
                    }
                    playlistItems.Add(playlistItem);
                }
                return playlistItems;
            }

            protected virtual PlaylistItem Read(int index, IDictionary<string, string> content)
            {
                var fileName = default(string);
                if (!content.TryGetValue(FILE, out fileName))
                {
                    return null;
                }
                var playlistItem = new PlaylistItem()
                {
                    DirectoryName = Path.GetDirectoryName(fileName),
                    FileName = fileName
                };
                playlistItem.MetaDatas = new List<MetaDataItem>()
                {
                    new MetaDataItem()
                    {
                        Name = CommonMetaData.Track,
                        Value = Convert.ToString(index),
                        Type = MetaDataItemType.Tag
                    }
                };
                foreach (var pair in content)
                {
                    if (string.Equals(pair.Key, FILE, StringComparison.OrdinalIgnoreCase))
                    {
                        //Already processed.
                    }
                    else if (string.Equals(pair.Key, LENGTH, StringComparison.OrdinalIgnoreCase))
                    {
                        //Length is in seconds.
                        var length = default(int);
                        if (!int.TryParse(pair.Value, out length) || length <= 1)
                        {
                            continue;
                        }
                        playlistItem.MetaDatas.Add(new MetaDataItem()
                        {
                            Name = CommonProperties.Duration,
                            Value = Convert.ToString(length * 1000),
                            Type = MetaDataItemType.Tag
                        });
                    }
                    else
                    {
                        //It looks like only File, Length and Title are allowed but let's read anything else.
                        var name = default(string);
                        if (!CommonMetaData.Lookup.TryGetValue(pair.Key, out name))
                        {
                            name = pair.Key;
                        }
                        var value = pair.Value;
                        playlistItem.MetaDatas.Add(new MetaDataItem()
                        {
                            Name = name,
                            Value = value,
                            Type = MetaDataItemType.Tag
                        });
                    }
                }
                return playlistItem;
            }

            public static Reader FromFile(string fileName)
            {
                return new Reader(File.ReadAllLines(fileName));
            }
        }

        public class Writer : IDisposable
        {
            protected static ILogger Logger
            {
                get
                {
                    return LogManager.Logger;
                }
            }

            public Writer(IEnumerable<PlaylistItem> content, StreamWriter writer)
            {
                this.Content = content;
                this.StreamWriter = writer;
            }

            public IEnumerable<PlaylistItem> Content { get; private set; }

            public StreamWriter StreamWriter { get; private set; }

            public void Write()
            {
                this.StreamWriter.WriteLine("[playlist]");
                var count = 0;
                foreach (var playlistItem in this.Content)
                {
                    this.Write(playlistItem, ++count);
                }
                this.StreamWriter.WriteLine("NumberOfEntries={0}", count);
                this.StreamWriter.WriteLine("Version=2");
            }

            protected virtual void Write(PlaylistItem playlistItem, int index)
            {
                var metaData = default(IDictionary<string, string>);
                if (playlistItem.MetaDatas != null)
                {
                    lock (playlistItem.MetaDatas)
                    {
                        metaData = playlistItem.MetaDatas.ToDictionary(
                            metaDataItem => metaDataItem.Name,
                            metaDataItem => metaDataItem.Value,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                }
                var title = metaData.GetValueOrDefault(CommonMetaData.Title);
                var duration = metaData.GetValueOrDefault(CommonProperties.Duration);
                this.StreamWriter.WriteLine("File{0}={1}", index, playlistItem.FileName);
                if (!string.IsNullOrEmpty(title))
                {
                    this.StreamWriter.WriteLine("Title{0}={1}", index, title);
                }
                if (!string.IsNullOrEmpty(duration))
                {
                    var length = default(int);
                    if (int.TryParse(duration, out length) && length > 0)
                    {
                        this.StreamWriter.WriteLine("Length{0}={1}", index, length / 1000);
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
                if (this.StreamWriter != null)
                {
                    this.StreamWriter.Dispose();
                }
            }

            ~Writer()
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

            public static Writer ToFile(IEnumerable<PlaylistItem> playlistItems, string fileName)
            {
                return new Writer(playlistItems, File.CreateText(fileName));
            }
        }

        public class PlaylistItemFactory : BaseComponent
        {
            public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.MetaDataSourceFactory = core.Factories.MetaDataSource;
                base.InitializeComponent(core);
            }

            public async Task<PlaylistItem[]> Create(IEnumerable<string> fileNames)
            {
                var playlistItems = new List<PlaylistItem>();
                foreach (var fileName in fileNames)
                {
                    playlistItems.AddRange(await this.Create(fileName).ConfigureAwait(false));
                }
                return playlistItems.ToArray();
            }

            public async Task<PlaylistItem[]> Create(string fileName)
            {
                var reader = Reader.FromFile(fileName);
                var playlistItems = reader.Read().ToArray();
                if (playlistItems.Any())
                {
                    var metaDataSource = this.MetaDataSourceFactory.Create();
                    foreach (var playlistItem in playlistItems)
                    {
                        try
                        {
                            var names = default(ISet<string>);
                            var metaData = await metaDataSource.GetMetaData(playlistItem.FileName).ConfigureAwait(false);
                            playlistItem.AddOrUpdate(metaData, out names);
                        }
                        catch (Exception e)
                        {
                            Logger.Write(this, LogLevel.Debug, "Failed to read meta data from file \"{0}\": {1}", fileName, e.Message);
                        }
                    }
                }
                return playlistItems;
            }
        }
    }
}
