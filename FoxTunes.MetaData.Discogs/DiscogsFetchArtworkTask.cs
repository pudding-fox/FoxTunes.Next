using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class DiscogsFetchArtworkTask : DiscogsLookupTask
    {
        private static readonly string PREFIX = typeof(DiscogsBehaviour).Name;

        public DiscogsFetchArtworkTask(Discogs.ReleaseLookup[] releaseLookups, MetaDataUpdateType updateType) : base(releaseLookups)
        {
            this.UpdateType = updateType;
        }

        public MetaDataUpdateType UpdateType { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = Strings.LookupArtworkTask_Name;
            return base.OnStarted();
        }

        protected override async Task<bool> OnLookupSuccess(Discogs.ReleaseLookup releaseLookup)
        {
            var result = default(bool);
            if (this.ShouldImportFrontCover(releaseLookup))
            {
                var frontCover = await this.ImportFrontCover(releaseLookup).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(frontCover))
                {
                    releaseLookup.MetaData[CommonImageTypes.FrontCover] = frontCover;
                    result = true;
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to download artwork for album {0} - {1}: Releases don't contain images or they count not be downloaded.", releaseLookup.Artist, releaseLookup.Album);
                    releaseLookup.AddError(Strings.DiscogsFetchArtworkTask_NotFound);
                    result = false;
                }
            }
            if (this.ShouldLookupArtist(releaseLookup))
            {
                var artist = await this.ImportArtist(releaseLookup).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(artist))
                {
                    releaseLookup.MetaData[CommonImageTypes.Artist] = artist;
                    result = true;
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to download artwork for album {0} - {1}: Releases don't contain images or they count not be downloaded.", releaseLookup.Artist, releaseLookup.Album);
                    releaseLookup.AddError(Strings.DiscogsFetchArtworkTask_NotFound);
                    result = false;
                }
            }
            return result;
        }

        protected virtual bool ShouldImportFrontCover(Discogs.ReleaseLookup releaseLookup)
        {
            return !this.HasMetaData(releaseLookup.FileDatas, CommonImageTypes.FrontCover, MetaDataItemType.Image);
        }

        protected virtual bool ShouldLookupArtist(Discogs.ReleaseLookup releaseLookup)
        {
            return !this.HasMetaData(releaseLookup.FileDatas, CommonImageTypes.Artist, MetaDataItemType.Image);
        }

        protected virtual bool HasMetaData(IEnumerable<IFileData> fileDatas, string name, MetaDataItemType type)
        {
            var result = true;
            foreach (var fileData in fileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    var metaDataItem = fileData.MetaDatas.FirstOrDefault(
                         element => string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) && element.Type == type
                    );
                    if (metaDataItem == null)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        protected virtual Task<string> ImportFrontCover(Discogs.ReleaseLookup releaseLookup)
        {
            var urls = new[]
            {
                releaseLookup.Release.CoverUrl,
                releaseLookup.Release.ThumbUrl
            };
            return this.FetchData(releaseLookup, urls);
        }

        protected virtual async Task<string> ImportArtist(Discogs.ReleaseLookup releaseLookup)
        {
            var release = default(Discogs.ReleaseDetails);
            try
            {
                Logger.Write(this, LogLevel.Debug, "Fetching release details: {0}", releaseLookup.Release.ResourceUrl);
                release = await this.Discogs.GetRelease(releaseLookup.Release).ConfigureAwait(false);
                if (release == null)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to fetch release details \"{0}\": Unknown error.", releaseLookup.Release.ResourceUrl);
                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to fetch release details \"{0}\": {1}", releaseLookup.Release.ResourceUrl, e.Message);
                releaseLookup.AddError(e.Message);
            }
            var urls = release.Artists.Select(
                artist => artist.ThumbnailUrl
            );
            return await this.FetchData(releaseLookup, urls);
        }

        protected virtual async Task<string> FetchData(Discogs.ReleaseLookup releaseLookup, IEnumerable<string> urls)
        {
            foreach (var url in urls)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    try
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
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            return fileName;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Error, "Failed to download data from url \"{0}\": {1}", url, e.Message);
                        releaseLookup.AddError(e.Message);
                    }
                }
            }
            return null;
        }
    }
}
