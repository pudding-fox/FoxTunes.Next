using FoxTunes.Interfaces;
using SacdSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class SacdItemFactory<T> : PopulatorBase where T : IFileData, new()
    {
        const string FILE_PREFIX = "bass_sacd.";

        const string FILE_SUFFIX = ".dsf";

        public SacdItemFactory(BackgroundTask task) : base(task.Visible)
        {
            this.Task = task;
        }

        public BackgroundTask Task { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Area { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            this.Configuration = core.Components.Configuration;
            this.Area = this.Configuration.GetElement<SelectionConfigurationElement>(
                BassSacdBehaviourConfiguration.SECTION,
                BassSacdBehaviourConfiguration.AREA
            );
            base.InitializeComponent(core);
        }

        public async Task<T[]> Create(IEnumerable<string> paths)
        {
            if (this.ReportProgress)
            {
                this.Name = Strings.SacdItemFactory_Name;
            }

            var items = new List<T>();
            foreach (var path in paths)
            {
                var sacd = SacdFactory.Instance.Create(path);
                sacd.InitialiseComponent();
                foreach (var area in sacd.Areas)
                {
                    var areaDescription = default(string);
                    if (area.Info.TryGetValue(global::SacdSharp.Constants.AREA_DESCRIPTION, out areaDescription))
                    {
                        var stereo = string.Equals(areaDescription, global::SacdSharp.Constants.STEREO, StringComparison.OrdinalIgnoreCase);
                        if (this.Area.Value.Id == BassSacdBehaviourConfiguration.AREA_STEREO && !stereo)
                        {
                            continue;
                        }
                        else if (this.Area.Value.Id == BassSacdBehaviourConfiguration.AREA_MULTI_CHANNEL && stereo)
                        {
                            continue;
                        }
                    }
                    this.Count = area.Tracks.Count * 100;
                    foreach (var track in area.Tracks)
                    {
                        await this.Create(sacd, area, track, path, items).ConfigureAwait(false);
                        if (this.Task.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            }
            return items.ToArray();
        }

        protected virtual async Task Create(Sacd sacd, SacdArea area, SacdTrack track, string path, List<T> items)
        {
            var metaDataSource = this.MetaDataSourceFactory.Create();
            if (this.ReportProgress)
            {
                var title = default(string);
                if (!track.Info.TryGetValue(CommonMetaData.Title, out title))
                {
                    title = string.Concat("Track ", area.Tracks.IndexOf(track) + 1);
                }
                this.Description = title;
            }
            var directoryName = Path.GetTempPath();
            var extractor = SacdFactory.Instance.Create(sacd, area, track);
            extractor.Progress += (sender, e) =>
            {
                this.Position = (area.Tracks.IndexOf(track) * 100) + e.Value;
                if (this.Task.IsCancellationRequested)
                {
                    extractor.Cancel();
                }
            };
            var inputFileName = extractor.GetFileName(directoryName);
            var outputFileName = Path.Combine(
               directoryName,
               string.Concat(FILE_PREFIX, Math.Abs(inputFileName.GetHashCode()), FILE_SUFFIX)
           );
            if (!File.Exists(outputFileName))
            {
                if (!File.Exists(inputFileName))
                {
                    if (!extractor.Extract(directoryName, out inputFileName))
                    {
                        return;
                    }
                }
                File.Move(inputFileName, outputFileName);
            }
            var item = new T()
            {
                DirectoryName = path,
                FileName = outputFileName
            };
            item.MetaDatas = (
                await metaDataSource.GetMetaData(outputFileName).ConfigureAwait(false)
            ).ToList();
            this.EnsureMetaData(outputFileName, sacd, area, track, item);
            items.Add(item);
            this.Position = (area.Tracks.IndexOf(track) + 1) * 100;
        }

        protected virtual void EnsureMetaData(string path, Sacd sacd, SacdArea area, SacdTrack track, T item)
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
                    Value = (area.Tracks.IndexOf(track) + 1).ToString()
                });
            }
            if (!hasTitle)
            {
                item.MetaDatas.Add(new MetaDataItem(CommonMetaData.Title, MetaDataItemType.Tag)
                {
                    Value = Path.GetFileNameWithoutExtension(path)
                });
            }
            item.MetaDatas.Add(new MetaDataItem(CustomMetaData.SourceFileName, MetaDataItemType.Tag)
            {
                Value = path
            });
        }

        public static void Cleanup()
        {
            var directoryName = Path.GetTempPath();
            var fileNames = Directory.GetFiles(directoryName, string.Concat(FILE_PREFIX, "*", FILE_SUFFIX));
            foreach (var fileName in fileNames)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                    //Nothing can be done.
                }
            }
        }
    }
}
