using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class DiscogsFetchTagsTask : DiscogsLookupTask
    {
        public DiscogsFetchTagsTask(Discogs.ReleaseLookup[] releaseLookups, MetaDataUpdateType updateType) : base(releaseLookups)
        {
            this.UpdateType = updateType;
        }

        public MetaDataUpdateType UpdateType { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = Strings.LookupTagsTask_Name;
            return base.OnStarted();
        }

        protected override Task<bool> OnLookupSuccess(Discogs.ReleaseLookup releaseLookup)
        {
            return this.ImportTags(releaseLookup);
        }

        protected virtual async Task<bool> ImportTags(Discogs.ReleaseLookup releaseLookup)
        {
            var release = default(Discogs.ReleaseDetails);
            try
            {
                Logger.Write(this, LogLevel.Debug, "Fetching release details: {0}", releaseLookup.Release.ResourceUrl);
                release = await this.Discogs.GetRelease(releaseLookup.Release).ConfigureAwait(false);
                if (release == null)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to fetch release details \"{0}\": Unknown error.", releaseLookup.Release.ResourceUrl);
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to fetch release details \"{0}\": {1}", releaseLookup.Release.ResourceUrl, e.Message);
                releaseLookup.AddError(e.Message);
            }
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fileData in releaseLookup.FileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    fileData.AddOrUpdate(metaData =>
                    {
                        var track = this.GetTrackDetails(fileData, metaData, release);
                        return this.GetMetaData(fileData, release, track);
                    }, names);
                }
            }
            if (names.Any())
            {
                var flags = MetaDataUpdateFlags.None;
                if (this.WriteTags.Value)
                {
                    flags |= MetaDataUpdateFlags.WriteToFiles;
                }
                await this.MetaDataManager.Save(releaseLookup.FileDatas, names, this.UpdateType, flags).ConfigureAwait(false);
            }
            return true;
        }

        protected virtual IEnumerable<MetaDataItem> GetMetaData(IFileData fileData, Discogs.ReleaseDetails releaseDetails, Discogs.TrackDetails trackDetails)
        {
            var artist = default(string);
            var album = default(string);
            var track = default(int);
            var title = default(string);
            var performer = default(string);
            var genre = default(string);
            var year = default(int);
            var isCompilation = default(bool);

            if (releaseDetails.Artists.Any())
            {
                //TODO: Multiple artists.
                artist = releaseDetails.Artists.First().Name;
            }
            album = releaseDetails.Title;
            if (trackDetails != null)
            {
                if (!int.TryParse(trackDetails.Position, out track))
                {
                    //TODO: We're assuming that the tracks are in order.
                    track = releaseDetails.Tracks.IndexOf(trackDetails) + 1;
                }
                title = trackDetails.Title;
                if (trackDetails.Artists.Any())
                {
                    //TODO: Multiple artists.
                    var trackArtist = trackDetails.Artists.First().Name;
                    if (string.IsNullOrEmpty(artist))
                    {
                        artist = trackArtist;
                    }
                    else if (!string.Equals(artist, trackArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        performer = trackArtist;
                        //TODO: Is it though?
                        isCompilation = true;
                    }
                }
            }
            genre = releaseDetails.Genres.FirstOrDefault();
            if (!int.TryParse(releaseDetails.Year, out year))
            {
                year = default(int);
            }

            var metaData = new List<MetaDataItem>();
            if (!string.IsNullOrEmpty(artist))
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Artist, MetaDataItemType.Tag) { Value = artist });
            }
            if (!string.IsNullOrEmpty(album))
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Album, MetaDataItemType.Tag) { Value = album });
            }
            if (track > 0)
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Track, MetaDataItemType.Tag) { Value = track.ToString() });
            }
            if (!string.IsNullOrEmpty(title))
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Title, MetaDataItemType.Tag) { Value = title });
            }
            if (!string.IsNullOrEmpty(performer))
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Performer, MetaDataItemType.Tag) { Value = performer });
            }
            if (!string.IsNullOrEmpty(genre))
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Genre, MetaDataItemType.Tag) { Value = genre });
            }
            if (year > 0)
            {
                metaData.Add(new MetaDataItem(CommonMetaData.Year, MetaDataItemType.Tag) { Value = year.ToString() });
            }
            if (isCompilation)
            {
                metaData.Add(new MetaDataItem(CommonMetaData.IsCompilation, MetaDataItemType.Tag) { Value = bool.TrueString });
            }
            return metaData;
        }

        protected virtual int GetTrackNumber(IFileData fileData, IDictionary<string, MetaDataItem> metaData)
        {
            var metaDataItem = default(MetaDataItem);
            if (metaData.TryGetValue(CommonMetaData.Track, out metaDataItem))
            {
                var track = default(int);
                if (int.TryParse(metaDataItem.Value, out track))
                {
                    return track;
                }
            }
            Logger.Write(this, LogLevel.Warn, "No track number found: {0}", fileData.FileName);
            return default(int);
        }

        protected virtual string GetTrackTitle(IFileData fileData, IDictionary<string, MetaDataItem> metaData)
        {
            var metaDataItem = default(MetaDataItem);
            if (metaData.TryGetValue(CommonMetaData.Title, out metaDataItem))
            {
                return metaDataItem.Value;
            }
            Logger.Write(this, LogLevel.Warn, "No track title found: {0}", fileData.FileName);
            return string.Empty;
        }

        protected virtual Discogs.TrackDetails GetTrackDetails(IFileData fileData, IDictionary<string, MetaDataItem> metaData, Discogs.ReleaseDetails releaseDetails)
        {
            return this.GetTrackDetails(
                fileData,
                this.GetTrackNumber(fileData, metaData),
                this.GetTrackTitle(fileData, metaData),
                releaseDetails
            );
        }

        protected virtual Discogs.TrackDetails GetTrackDetails(IFileData fileData, int track, string title, Discogs.ReleaseDetails releaseDetails)
        {
            //If no track number was provided but the release contains only one track then return it.
            if (track == default(int))
            {
                if (releaseDetails.Tracks.Length == 1)
                {
                    return releaseDetails.Tracks.First();
                }
            }
            foreach (var trackDetails in releaseDetails.Tracks)
            {
                //If no track numer was provided then attempt a title match.
                if (track == default(int))
                {
                    if (!string.IsNullOrEmpty(title) && Discogs.Sanitize(title).Similarity(Discogs.Sanitize(trackDetails.Title), true) >= this.MinConfidence.Value)
                    {
                        return trackDetails;
                    }
                }
                //Otherwise attept a position match.
                else
                {
                    var position = default(int);
                    if (int.TryParse(trackDetails.Position, out position))
                    {
                        if (position == track)
                        {
                            return trackDetails;
                        }
                    }
                    else
                    {
                        //TODO: Position is not always numeric.
                    }
                }
            }
            Logger.Write(this, LogLevel.Warn, "No track details found: {0}", fileData.FileName);
            return default(Discogs.TrackDetails);
        }
    }
}
