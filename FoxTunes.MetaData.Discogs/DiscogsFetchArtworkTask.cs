using FoxTunes.Interfaces;
using System;
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
            var value = await this.ImportImage(releaseLookup).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(value))
            {
                releaseLookup.MetaData[CommonImageTypes.FrontCover] = value;
                return true;
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Failed to download artwork for album {0} - {1}: Releases don't contain images or they count not be downloaded.", releaseLookup.Artist, releaseLookup.Album);
                releaseLookup.AddError(Strings.DiscogsFetchArtworkTask_NotFound);
                return false;
            }
        }

        protected virtual async Task<string> ImportImage(Discogs.ReleaseLookup releaseLookup)
        {
            var urls = new[]
            {
                releaseLookup.Release.CoverUrl,
                releaseLookup.Release.ThumbUrl
            };
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
            return string.Empty;
        }
    }
}
