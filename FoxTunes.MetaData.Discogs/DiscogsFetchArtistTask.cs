using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class DiscogsFetchArtistTask : DiscogsTask
    {
        private static readonly string PREFIX = typeof(DiscogsBehaviour).Name;

        public DiscogsFetchArtistTask(IEnumerable<IFileData> fileDatas, MetaDataUpdateType updateType)
        {
            this.FileDatas = fileDatas;
            this.UpdateType = updateType;
            this.Values = new List<OnDemandMetaDataValue>();
        }

        public IEnumerable<IFileData> FileDatas { get; private set; }

        public MetaDataUpdateType UpdateType { get; private set; }

        public IList<OnDemandMetaDataValue> Values { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        public IMetaDataManager MetaDataManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataManager = core.Managers.MetaData;
            base.InitializeComponent(core);
        }

        protected override Task OnStarted()
        {
            this.Name = Strings.LookupArtworkTask_Name;
            return base.OnStarted();
        }

        protected override async Task OnRun()
        {
            var artistLookups = this.FileDatas.GroupBy(fileData =>
            {
                var metaData = default(IDictionary<string, string>);
                lock (fileData.MetaDatas)
                {
                    metaData = fileData.MetaDatas.ToDictionary2();
                }
                var artist = metaData.GetValueOrDefault(CommonMetaData.Artist);
                return artist;
            }).Where(pair => !string.IsNullOrEmpty(pair.Key)).ToArray();
            this.Count = artistLookups.Length;
            foreach (var artistLookup in artistLookups)
            {
                this.Description = artistLookup.Key;
                if (this.IsCancellationRequested)
                {
                    break;
                }
                await this.FetchData(artistLookup.Key, artistLookup);
                this.Position++;
            }
        }

        protected virtual async Task FetchData(string artist, IEnumerable<IFileData> fileDatas)
        {
            var result = await this.Discogs.GetArtist(artist);
            await this.SaveMetaData(result).ConfigureAwait(false);
            if (result != null)
            {
                foreach (var fileData in fileDatas)
                {
                    this.Values.Add(new OnDemandMetaDataValue(fileData, await this.FetchData(result.Url).ConfigureAwait(false)));
                }
            }
        }

        protected virtual async Task<string> FetchData(string url)
        {
            var fileName = await FileMetaDataStore.IfNotExistsAsync(PREFIX, url, async result =>
            {
                Logger.Write(this, LogLevel.Debug, "Downloading data from url: {0}", url);
                var data = await this.Discogs.GetData(url).ConfigureAwait(false);
                if (data == null)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to download data from url \"{0}\": Unknown error.", url);
                    return string.Empty;
                }
                return await FileMetaDataStore.WriteAsync(PREFIX, url, data).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return fileName;
        }

        protected virtual async Task SaveMetaData(Discogs.Artist artist)
        {
            var value = default(string);
            if (artist != null)
            {
                //TODO: This should be an Id but sometimes there isn't one. Weird.
                value = Convert.ToString(Math.Abs(artist.Url.GetDeterministicHashCode()));
            }
            else
            {
                value = Discogs.Artist.None;
            }
            foreach (var fileData in this.FileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    fileData.AddOrUpdate(CustomMetaData.DiscogsArtist, MetaDataItemType.Tag, value);
                }
                Logger.Write(this, LogLevel.Debug, "Discogs artist: {0} => {1}", fileData.FileName, value);
            }
            await this.MetaDataManager.Save(
                this.FileDatas,
                new[] { CustomMetaData.DiscogsArtist },
                MetaDataUpdateType.System,
                MetaDataUpdateFlags.None
            ).ConfigureAwait(false);
        }
    }
}
