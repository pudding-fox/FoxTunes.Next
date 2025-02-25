using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

namespace FoxTunes
{
    public class TagLibMetaDataSource : BaseComponent, IMetaDataSource
    {
        //10MB
        public static int MAX_TAG_SIZE = 10240000;

        public TagLibMetaDataSource()
        {
            this.Warnings = new ConcurrentDictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public ConcurrentDictionary<string, IList<string>> Warnings { get; private set; }

        public IEnumerable<string> GetWarnings(string fileName)
        {
            var warnings = default(IList<string>);
            if (!this.Warnings.TryGetValue(fileName, out warnings))
            {
                return Enumerable.Empty<string>();
            }
            return warnings;
        }

        public void AddWarning(string fileName, string warning)
        {
            this.Warnings.GetOrAdd(fileName, key => new List<string>()).Add(warning);
        }

        public void AddWarnings(string fileName, IEnumerable<string> warnings)
        {
            this.Warnings.GetOrAdd(fileName, key => new List<string>()).AddRange(warnings);
        }

        public TagLibFileFactory FileFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement EmbeddedImages { get; private set; }

        public BooleanConfigurationElement LooseImages { get; private set; }

        public SelectionConfigurationElement ImagesPreference { get; private set; }

        public BooleanConfigurationElement CopyImages { get; private set; }

        public IntegerConfigurationElement MaxImageSize { get; private set; }

        public BooleanConfigurationElement Extended { get; private set; }

        public BooleanConfigurationElement MusicBrainz { get; private set; }

        public BooleanConfigurationElement Lyrics { get; private set; }

        public BooleanConfigurationElement ReplayGain { get; private set; }

        public BooleanConfigurationElement Popularimeter { get; private set; }

        public BooleanConfigurationElement Documents { get; private set; }

        public BooleanConfigurationElement FileSystem { get; private set; }

        public SelectionConfigurationElement Write { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.FileFactory = ComponentRegistry.Instance.GetComponent<TagLibFileFactory>();
            this.Configuration = core.Components.Configuration;
            this.EmbeddedImages = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_EMBEDDED_IMAGES
            );
            this.LooseImages = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_LOOSE_IMAGES
            );
            this.ImagesPreference = this.Configuration.GetElement<SelectionConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.IMAGES_PREFERENCE
            );
            this.CopyImages = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.COPY_IMAGES_ELEMENT
            );
            this.MaxImageSize = this.Configuration.GetElement<IntegerConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.MAX_IMAGE_SIZE
            );
            this.Extended = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_EXTENDED_TAGS
            );
            this.MusicBrainz = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_MUSICBRAINZ_TAGS
            );
            this.Lyrics = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_LYRICS_TAGS
            );
            this.ReplayGain = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_REPLAY_GAIN_TAGS
            );
            this.Popularimeter = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_POPULARIMETER_TAGS
            );
            this.Documents = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_DOCUMENTS
            );
            this.FileSystem = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_FILESYSTEM
            );
            this.Write = this.Configuration.GetElement<SelectionConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.WRITE_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public Task<IEnumerable<MetaDataItem>> GetMetaData(string fileName)
        {
            return this.GetMetaData(fileName, () => this.FileFactory.Create(fileName));
        }

        public Task<IEnumerable<MetaDataItem>> GetMetaData(IFileAbstraction fileAbstraction)
        {
            return this.GetMetaData(fileAbstraction.FileName, () => this.FileFactory.Create(fileAbstraction));
        }

        public async Task<IEnumerable<MetaDataItem>> GetMetaData(string fileName, Func<File> factory)
        {
            var metaData = new List<MetaDataItem>();
            if (!this.IsSupported(fileName))
            {
                Logger.Write(this, LogLevel.Warn, "Unsupported file format: {0}", fileName);
                this.AddWarning(fileName, "Unsupported file format.");
                //Add minimal meta data so library is happy.
                if (this.FileSystem.Value)
                {
                    this.Try(() => FileSystemManager.Read(this, metaData, fileName), this.ErrorHandler);
                }
                return metaData;
            }
            var collect = default(bool);
            Logger.Write(this, LogLevel.Trace, "Reading meta data for file: {0}", fileName);
            try
            {
                using (var file = factory())
                {
                    if (file.PossiblyCorrupt)
                    {
                        this.AddWarnings(fileName, file.CorruptionReasons);
                    }
                    if (file.InvariantStartPosition > MAX_TAG_SIZE)
                    {
                        collect = true;
                    }
                    if (file.Tag != null)
                    {
                        this.AddTags(metaData, file.Tag);
                    }
                    if (file.Properties != null)
                    {
                        this.AddProperties(metaData, file.Properties);
                    }
                    if (this.FileSystem.Value)
                    {
                        this.Try(() => FileSystemManager.Read(this, metaData, file), this.ErrorHandler);
                    }
                    if (this.Popularimeter.Value)
                    {
                        this.Try(() => PopularimeterManager.Read(this, metaData, file), this.ErrorHandler);
                    }
                    if (this.ReplayGain.Value)
                    {
                        this.Try(() => ReplayGainManager.Read(this, metaData, file), this.ErrorHandler);
                    }
                    if (this.Documents.Value)
                    {
                        this.Try(() => DocumentManager.Read(this, metaData, file), this.ErrorHandler);
                    }
                    this.Try(() => CompilationManager.Read(this, metaData, file), this.ErrorHandler);
                    if (file is IMetaDataSource metaDataSource)
                    {
                        await this.AddAdditional(metaData, file, metaDataSource).ConfigureAwait(false);
                    }
                    await ImageManager.Read(this, metaData, file).ConfigureAwait(false);
                }
            }
            catch (UnsupportedFormatException)
            {
                Logger.Write(this, LogLevel.Warn, "Unsupported file format: {0}", fileName);
                this.AddWarning(fileName, "Unsupported file format.");
            }
            catch (OutOfMemoryException)
            {
                //This can happen with really big embedded images.
                //It's tricky to avoid because we can't check InvariantStartPosition without parsing.
                Logger.Write(this, LogLevel.Warn, "Out of memory: {0}", fileName);
                this.AddWarning(fileName, "Out of memory.");
                collect = true;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to read meta data: {0} => {1}", fileName, e.Message);
                this.AddWarning(fileName, string.Format("Failed to read meta data: {0}", e.Message));
            }
            finally
            {
                if (collect)
                {
                    //If we encountered a large meta data section (>10MB) then we need to try to reclaim the memory.
                    GC.Collect();
                }
            }
            return metaData;
        }

        public async Task SetMetaData(string fileName, IEnumerable<MetaDataItem> metaData, Func<MetaDataItem, bool> predicate)
        {
            if (MetaDataBehaviourConfiguration.GetWriteBehaviour(this.Write.Value) == WriteBehaviour.None)
            {
                Logger.Write(this, LogLevel.Warn, "Writing is disabled: {0}", fileName);
                return;
            }
            if (!this.IsSupported(fileName))
            {
                Logger.Write(this, LogLevel.Warn, "Unsupported file format: {0}", fileName);
                return;
            }
            var metaDataItems = default(IEnumerable<MetaDataItem>);
            lock (metaData)
            {
                if (predicate != null)
                {
                    metaDataItems = metaData.Where(predicate).ToArray();
                }
                else
                {
                    metaDataItems = metaData.ToArray();
                }
                if (!metaDataItems.Any())
                {
                    //Nothing to update.
                    return;
                }
            }
            var collect = default(bool);
            using (var file = this.FileFactory.Create(fileName))
            {
                if (file.PossiblyCorrupt)
                {
                    this.AddWarnings(fileName, file.CorruptionReasons);
                }
                foreach (var metaDataItem in metaDataItems)
                {
                    switch (metaDataItem.Type)
                    {
                        case MetaDataItemType.Tag:
                            this.SetTag(metaDataItem, file);
                            break;
                        case MetaDataItemType.Image:
                            if (file.InvariantStartPosition > MAX_TAG_SIZE)
                            {
                                collect = true;
                            }
                            await ImageManager.Write(this, metaDataItem, file).ConfigureAwait(false);
                            break;
                        case MetaDataItemType.Document:
                            if (this.Documents.Value)
                            {
                                DocumentManager.Write(this, metaDataItem, file);
                            }
                            break;
                    }
                }
                if (file is IMetaDataSource metaDataSource)
                {
                    await this.SetAdditional(metaData, file, metaDataSource).ConfigureAwait(false);
                }
                file.Save();
            }
            if (collect)
            {
                //If we encountered a large meta data section (>10MB) then we need to try to reclaim the memory.
                GC.Collect();
            }
        }

        protected virtual bool IsSupported(string fileName)
        {
            var mimeType = string.Format("taglib/{0}", fileName.GetExtension());
            return FileTypes.AvailableTypes.ContainsKey(mimeType);
        }

        private void AddTags(IList<MetaDataItem> metaData, Tag tag)
        {
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Album, tag.Album), this.ErrorHandler);
            (this).Try(() =>
            {
                if (!string.IsNullOrEmpty(tag.FirstAlbumArtist))
                {
                    this.AddTag(metaData, CommonMetaData.Artist, tag.FirstAlbumArtist);
                }
                else if (!string.IsNullOrEmpty(tag.FirstPerformer))
                {
                    this.AddTag(metaData, CommonMetaData.Artist, tag.FirstPerformer);
                }
            }, this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Composer, tag.FirstComposer), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Conductor, tag.Conductor), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Disc, tag.Disc.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.DiscCount, tag.DiscCount.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Genre, tag.FirstGenre), this.ErrorHandler);
            (this).Try(() =>
            {
                if (!string.Equals(tag.FirstAlbumArtist, tag.FirstPerformer, StringComparison.OrdinalIgnoreCase))
                {
                    this.AddTag(metaData, CommonMetaData.Performer, tag.FirstPerformer);
                }
            }, this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Title, tag.Title), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Track, tag.Track.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.TrackCount, tag.TrackCount.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.Year, tag.Year.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.BeatsPerMinute, tag.BeatsPerMinute.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddTag(metaData, CommonMetaData.InitialKey, tag.InitialKey), this.ErrorHandler);

            if (this.Extended.Value)
            {
                (this).Try(() => this.AddTag(metaData, ExtendedMetaData.MusicIpId, tag.MusicIpId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, ExtendedMetaData.AmazonId, tag.AmazonId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, ExtendedMetaData.Comment, tag.Comment), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, ExtendedMetaData.Copyright, tag.Copyright), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, ExtendedMetaData.Grouping, tag.Grouping), this.ErrorHandler);
            }

            if (this.MusicBrainz.Value)
            {
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzArtistId, tag.MusicBrainzArtistId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzDiscId, tag.MusicBrainzDiscId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzReleaseArtistId, tag.MusicBrainzReleaseArtistId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzReleaseCountry, tag.MusicBrainzReleaseCountry), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzReleaseId, tag.MusicBrainzReleaseId), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzReleaseStatus, tag.MusicBrainzReleaseStatus), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzReleaseType, tag.MusicBrainzReleaseType), this.ErrorHandler);
                (this).Try(() => this.AddTag(metaData, MusicBrainzMetaData.MusicBrainzTrackId, tag.MusicBrainzTrackId), this.ErrorHandler);
            }

            if (this.Lyrics.Value)
            {
                (this).Try(() => this.AddTag(metaData, CommonMetaData.Lyrics, tag.Lyrics), this.ErrorHandler);
            }
        }

        private bool HasValue(string value)
        {
            return !string.IsNullOrEmpty(value) && !string.Equals(value, 0.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public void AddTag(IList<MetaDataItem> metaData, string name, string value)
        {
            if (!this.HasValue(value))
            {
                return;
            }
            metaData.Add(new MetaDataItem(name, MetaDataItemType.Tag) { Value = value.Trim() });
        }

        private void AddProperties(IList<MetaDataItem> metaData, Properties properties)
        {
            (this).Try(() => this.AddProperty(metaData, CommonProperties.Description, properties.Description), this.ErrorHandler);
            (this).Try(() => this.AddProperty(metaData, CommonProperties.Duration, properties.Duration.TotalMilliseconds.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddProperty(metaData, CommonProperties.AudioBitrate, properties.AudioBitrate.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddProperty(metaData, CommonProperties.AudioChannels, properties.AudioChannels.ToString()), this.ErrorHandler);
            (this).Try(() => this.AddProperty(metaData, CommonProperties.AudioSampleRate, properties.AudioSampleRate.ToString()), this.ErrorHandler);
            (this).Try(() =>
            {
                if (properties.BitsPerSample != 0)
                {
                    this.AddProperty(metaData, CommonProperties.BitsPerSample, properties.BitsPerSample.ToString());
                }
                else
                {
                    //This is special case just for MPEG-4.
                    foreach (var codec in properties.Codecs.OfType<TagLib.Mpeg4.IsoAudioSampleEntry>())
                    {
                        if (codec.AudioSampleSize != 0)
                        {
                            this.AddProperty(metaData, CommonProperties.BitsPerSample, codec.AudioSampleSize.ToString());
                            break;
                        }
                    }
                }
            }, this.ErrorHandler);
        }

        private void AddProperty(IList<MetaDataItem> metaData, string name, string value)
        {
            if (!this.HasValue(value))
            {
                return;
            }
            metaData.Add(new MetaDataItem(name, MetaDataItemType.Property) { Value = value.Trim() });
        }

        private async Task AddAdditional(IList<MetaDataItem> metaData, File file, IMetaDataSource metaDataSource)
        {
            try
            {
                metaData.AddRange(await metaDataSource.GetMetaData(file.Name).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                this.ErrorHandler(e);
            }
        }

        private void SetTag(MetaDataItem metaDataItem, File file)
        {
            var tag = file.Tag;
            if (string.Equals(metaDataItem.Name, CommonMetaData.Album, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Album = metaDataItem.Value, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Artist, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.AlbumArtists = new[] { metaDataItem.Value }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Composer, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Composers = new[] { metaDataItem.Value }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Conductor, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Conductor = metaDataItem.Value, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Disc, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() =>
                {
                    var disc = default(uint);
                    if (uint.TryParse(metaDataItem.Value, out disc))
                    {
                        tag.Disc = disc;
                    }
                    else
                    {
                        tag.Disc = 0;
                    }
                }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.DiscCount, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() =>
                {
                    var discCount = default(uint);
                    if (uint.TryParse(metaDataItem.Value, out discCount))
                    {
                        tag.DiscCount = discCount;
                    }
                    else
                    {
                        tag.DiscCount = 0;
                    }
                }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Genre, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Genres = new[] { metaDataItem.Value }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonStatistics.LastPlayed, StringComparison.OrdinalIgnoreCase))
            {
                if (this.Popularimeter.Value && MetaDataBehaviourConfiguration.GetWriteBehaviour(this.Write.Value).HasFlag(WriteBehaviour.Statistics))
                {
                    this.Try(() => PopularimeterManager.Write(this, metaDataItem, file), this.ErrorHandler);
                }
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Performer, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Performers = new[] { metaDataItem.Value }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonStatistics.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                if (this.Popularimeter.Value && MetaDataBehaviourConfiguration.GetWriteBehaviour(this.Write.Value).HasFlag(WriteBehaviour.Statistics))
                {
                    this.Try(() => PopularimeterManager.Write(this, metaDataItem, file), this.ErrorHandler);
                }
            }
            else if (string.Equals(metaDataItem.Name, CommonStatistics.Rating, StringComparison.OrdinalIgnoreCase))
            {
                if (this.Popularimeter.Value)
                {
                    this.Try(() => PopularimeterManager.Write(this, metaDataItem, file), this.ErrorHandler);
                }
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainAlbumGain, StringComparison.OrdinalIgnoreCase) || string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainAlbumPeak, StringComparison.OrdinalIgnoreCase) || string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainTrackGain, StringComparison.OrdinalIgnoreCase) || string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainTrackPeak, StringComparison.OrdinalIgnoreCase))
            {
                if (this.ReplayGain.Value)
                {
                    this.Try(() => ReplayGainManager.Write(this, metaDataItem, file), this.ErrorHandler);
                }
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Title, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => tag.Title = metaDataItem.Value, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Track, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() =>
                {
                    var track = default(uint);
                    if (uint.TryParse(metaDataItem.Value, out track))
                    {
                        tag.Track = track;
                    }
                    else
                    {
                        tag.Track = 0;
                    }
                }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.TrackCount, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() =>
                {
                    var trackCount = default(uint);
                    if (uint.TryParse(metaDataItem.Value, out trackCount))
                    {
                        tag.TrackCount = trackCount;
                    }
                    else
                    {
                        tag.TrackCount = 0;
                    }
                }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.Year, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() =>
                {
                    var year = default(uint);
                    if (uint.TryParse(metaDataItem.Value, out year))
                    {
                        tag.Year = year;
                    }
                    else
                    {
                        tag.Year = 0;
                    }
                }, this.ErrorHandler);
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.IsCompilation, StringComparison.OrdinalIgnoreCase))
            {
                this.Try(() => CompilationManager.Write(this, metaDataItem, file), this.ErrorHandler);
            }
        }

        private Task SetAdditional(IEnumerable<MetaDataItem> metaData, File file, IMetaDataSource metaDataSource)
        {
            return metaDataSource.SetMetaData(file.Name, metaData, metaDataItem => true);
        }

        private void ErrorHandler(Exception e)
        {
            Logger.Write(this, LogLevel.Error, "Failed to read meta data: {0}", e.Message);
        }
    }

    public static class MusicBrainzReleaseType
    {
        public const string Compilation = "compilation";
    }
}
