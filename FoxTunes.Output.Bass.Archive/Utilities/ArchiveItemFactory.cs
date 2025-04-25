using FoxTunes.Interfaces;
using ManagedBass.ZipStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class ArchiveItemFactory<T> : PopulatorBase where T : IFileData, new()
    {
        public ArchiveItemFactory(bool reportProgress) : base(reportProgress)
        {

        }

        public IOutput Output { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public BassArchiveStreamPasswordBehaviour PasswordBehaviour { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement MetaData { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            this.PasswordBehaviour = ComponentRegistry.Instance.GetComponent<BassArchiveStreamPasswordBehaviour>();
            this.Configuration = core.Components.Configuration;
            this.MetaData = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.METADATA_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public async Task<T[]> Create(IEnumerable<string> paths)
        {
            if (this.ReportProgress)
            {
                this.Name = Strings.ArchiveItemFactory_Name;
            }

            var items = new List<T>();
            foreach (var path in paths)
            {
                var archive = default(IntPtr);
                if (Archive.Create(out archive))
                {
                    try
                    {
                        if (Archive.Open(archive, path))
                        {
                            await this.Create(archive, path, items).ConfigureAwait(false);
                        }
                        else
                        {
                            //TODO: Warn.
                        }
                    }
                    finally
                    {
                        Archive.Release(archive);
                    }
                }
                else
                {
                    //TODO: Warn.
                }
            }
            return items.ToArray();
        }

        protected virtual async Task Create(IntPtr archive, string path, List<T> items)
        {
            var count = default(int);
            var metaDataSource = this.MetaDataSourceFactory.Create();
            if (Archive.GetEntryCount(archive, out count))
            {
                for (var a = 0; a < count; a++)
                {
                    var entry = default(Archive.ArchiveEntry);
                    if (Archive.GetEntry(archive, out entry, a))
                    {
                        if (!this.Output.IsSupported(entry.path))
                        {
                            continue;
                        }
                        if (this.ReportProgress)
                        {
                            this.Description = Path.GetFileName(entry.path);
                        }
                        var fileName = ArchiveUtils.CreateUrl(path, entry.path);
                        var item = new T()
                        {
                            //An archive is a virtual directory I suppose? Not sure if this will cause any problems.
                            DirectoryName = path,
                            FileName = fileName
                        };
                        if (this.MetaData.Value)
                        {
                            try
                            {
                            retry:
                                using (var fileAbstraction = ArchiveFileAbstraction.Create(path, entry.path, a))
                                {
                                    if (fileAbstraction.IsOpen)
                                    {
                                        item.MetaDatas = (
                                            await metaDataSource.GetMetaData(fileAbstraction).ConfigureAwait(false)
                                        ).ToList();
                                        switch (fileAbstraction.Result)
                                        {
                                            case ArchiveEntry.RESULT_PASSWORD_REQUIRED:
                                                Logger.Write(this, LogLevel.Warn, "Invalid password for \"{0}\".", path);
                                                if (this.PasswordBehaviour != null && !this.PasswordBehaviour.WasCancelled(path))
                                                {
                                                    this.PasswordBehaviour.Reset(path);
                                                    goto retry;
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Write(this, LogLevel.Debug, "Failed to read meta data from file \"{0}\": {1}", path, e.Message);
                            }
                        }
                        this.EnsureMetaData(path, a, entry, item);
                        items.Add(item);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to read archive entry at position {0}.", a);
                    }
                }
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Failed to read archive entries.");
            }
        }

        protected virtual void EnsureMetaData(string path, int index, Archive.ArchiveEntry entry, T item)
        {
            var hasTrack = false;
            var hasTitle = false;
            if (item.MetaDatas != null)
            {
                var metaData = item.MetaDatas.ToDictionary2();
                hasTrack = metaData.ContainsKey(CommonMetaData.Track);
                hasTitle = metaData.ContainsKey(CommonMetaData.Title);
            }
            else
            {
                item.MetaDatas = new List<MetaDataItem>();
            }
            if (!hasTrack)
            {
                item.MetaDatas.Add(new MetaDataItem(CommonMetaData.Track, MetaDataItemType.Tag)
                {
                    Value = Convert.ToString(index + 1)
                });
            }
            if (!hasTitle)
            {
                item.MetaDatas.Add(new MetaDataItem(CommonMetaData.Title, MetaDataItemType.Tag)
                {
                    Value = entry.path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).LastOrDefault()
                });
            }
            item.MetaDatas.Add(new MetaDataItem(CustomMetaData.SourceFileName, MetaDataItemType.Tag)
            {
                Value = path
            });
        }
    }
}
