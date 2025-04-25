using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class SelectionProperties : ConfigurableViewModelBase
    {
        const int TIMEOUT = 100;

        private static readonly string[] TAGS = new[]
        {
            CommonMetaData.Album,
            CommonMetaData.Artist,
            CommonMetaData.Composer,
            CommonMetaData.Conductor,
            CommonMetaData.Disc,
            CommonMetaData.DiscCount,
            CommonMetaData.Genre,
            CommonMetaData.Performer,
            CommonMetaData.Title,
            CommonMetaData.Track,
            CommonMetaData.TrackCount,
            CommonMetaData.Year,
            CommonMetaData.IsCompilation
        };

        private static readonly string[] PROPERTIES = new[]
        {
            CountValueProvider.NAME,
            CommonProperties.AudioBitrate,
            CommonProperties.AudioChannels,
            CommonProperties.AudioSampleRate,
            CommonProperties.BitsPerSample,
            CommonProperties.Duration,
            CommonProperties.Description,
        };

        private static readonly string[] REPLAYGAIN = new[]
        {
            CommonMetaData.ReplayGainTrackGain,
            CommonMetaData.ReplayGainTrackPeak,
            CommonMetaData.ReplayGainAlbumGain,
            CommonMetaData.ReplayGainAlbumPeak
        };

        private static readonly string[] FILESYSTEM = new[]
        {
            FileSystemProperties.FileName,
            FileSystemProperties.DirectoryName,
            FileSystemProperties.FileSize,
            FileSystemProperties.FileCreationTime,
            FileSystemProperties.FileModificationTime
        };

        private static readonly string[] IMAGES = new[]
        {
            CommonImageTypes.FrontCover
        };

        private static readonly IDictionary<string, ValueProvider> PROVIDERS = new Dictionary<string, ValueProvider>(StringComparer.OrdinalIgnoreCase)
        {
            { CommonMetaData.IsCompilation, BooleanMetaDataValueProvider.Instance },
            { CountValueProvider.NAME, CountValueProvider.Instance },
            { CommonProperties.AudioBitrate, BitrateValueProvider.Instance },
            { CommonProperties.AudioSampleRate, SamplerateValueProvider.Instance },
            { CommonProperties.Duration, NumericMetaDataValueProvider.Instance },
            { CommonMetaData.ReplayGainAlbumGain, DecibelValueProvider.Instance },
            { CommonMetaData.ReplayGainTrackGain, DecibelValueProvider.Instance },
            { CommonImageTypes.FrontCover, MetaDataValueProvider.Instance },
        };

        private static readonly IDictionary<string, ValueAggregator> AGGREGATORS = new Dictionary<string, ValueAggregator>(StringComparer.OrdinalIgnoreCase)
        {
            { CountValueAggregator.NAME, CountValueAggregator.Instance },
            { CommonProperties.Duration, TimeSpanValueAggregator.Instance },
            { FileSystemProperties.FileSize, FileSystemUsageValueAggregator.Instance },
        };

        public SelectionProperties() : base(false)
        {
            this.Debouncer = new Debouncer(TIMEOUT);
            this.FileDatas = new ObservableCollection<IFileData>();
            this.Tags = new ObservableCollection<Row>();
            this.Properties = new ObservableCollection<Row>();
            this.ReplayGain = new ObservableCollection<Row>();
            this.FileSystem = new ObservableCollection<Row>();
            this.Images = new ObservableCollection<Row>();
            if (Core.Instance != null)
            {
                this.InitializeComponent(Core.Instance);
            }
        }

        public Debouncer Debouncer { get; private set; }

        public object FileDatasSource { get; private set; }

        public ObservableCollection<IFileData> FileDatas { get; private set; }

        public ObservableCollection<Row> Tags { get; private set; }

        public ObservableCollection<Row> Properties { get; private set; }

        public ObservableCollection<Row> ReplayGain { get; private set; }

        public ObservableCollection<Row> FileSystem { get; private set; }

        public ObservableCollection<Row> Images { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        private bool _ShowTags { get; set; }

        public bool ShowTags
        {
            get
            {
                return this._ShowTags;
            }
            set
            {
                this._ShowTags = value;
                this.OnShowTagsChanged();
            }
        }

        protected virtual void OnShowTagsChanged()
        {
            this.Debouncer.Exec(this.Refresh);
            if (this.ShowTagsChanged != null)
            {
                this.ShowTagsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowTags");
        }

        public event EventHandler ShowTagsChanged;

        private bool _ShowProperties { get; set; }

        public bool ShowProperties
        {
            get
            {
                return this._ShowProperties;
            }
            set
            {
                this._ShowProperties = value;
                this.OnShowPropertiesChanged();
            }
        }

        protected virtual void OnShowPropertiesChanged()
        {
            this.Debouncer.Exec(this.Refresh);
            if (this.ShowPropertiesChanged != null)
            {
                this.ShowPropertiesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowProperties");
        }

        public event EventHandler ShowPropertiesChanged;

        private bool _ShowReplayGain { get; set; }

        public bool ShowReplayGain
        {
            get
            {
                return this._ShowReplayGain;
            }
            set
            {
                this._ShowReplayGain = value;
                this.OnShowReplayGainChanged();
            }
        }

        protected virtual void OnShowReplayGainChanged()
        {
            this.Debouncer.Exec(this.Refresh);
            if (this.ShowReplayGainChanged != null)
            {
                this.ShowReplayGainChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowReplayGain");
        }

        public event EventHandler ShowReplayGainChanged;

        private bool _ShowImages { get; set; }

        public bool ShowImages
        {
            get
            {
                return this._ShowImages;
            }
            set
            {
                this._ShowImages = value;
                this.OnShowImagesChanged();
            }
        }

        protected virtual void OnShowImagesChanged()
        {
            this.Debouncer.Exec(this.Refresh);
            if (this.ShowImagesChanged != null)
            {
                this.ShowImagesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowImages");
        }

        public event EventHandler ShowImagesChanged;

        private bool _ShowLocation { get; set; }

        public bool ShowLocation
        {
            get
            {
                return this._ShowLocation;
            }
            set
            {
                this._ShowLocation = value;
                this.OnShowLocationChanged();
            }
        }

        protected virtual void OnShowLocationChanged()
        {
            this.Debouncer.Exec(this.Refresh);
            if (this.ShowLocationChanged != null)
            {
                this.ShowLocationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowLocation");
        }

        public event EventHandler ShowLocationChanged;

        private string _StatusMessage { get; set; }

        public string StatusMessage
        {
            get
            {
                return this._StatusMessage;
            }
            set
            {
                this._StatusMessage = value;
                this.OnStatusMessageChanged();
            }
        }

        protected virtual void OnStatusMessageChanged()
        {
            if (this.StatusMessageChanged != null)
            {
                this.StatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StatusMessage");
        }

        public event EventHandler StatusMessageChanged;

        private bool _HasStatusMessage { get; set; }

        public bool HasStatusMessage
        {
            get
            {
                return this._HasStatusMessage;
            }
            set
            {
                this._HasStatusMessage = value;
                this.OnHasStatusMessageChanged();
            }
        }

        protected virtual void OnHasStatusMessageChanged()
        {
            if (this.HasStatusMessageChanged != null)
            {
                this.HasStatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasStatusMessage");
        }

        public event EventHandler HasStatusMessageChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.LibraryManager.SelectedItemChanged += this.OnSelectedItemChanged;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.PlaylistManager.SelectedItemsChanged += this.OnSelectedItemsChanged;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Debouncer.Exec(this.Refresh);
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_TAGS
                ).ConnectValue(value => this.ShowTags = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_PROPERTIES
                ).ConnectValue(value => this.ShowProperties = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_REPLAYGAIN
                ).ConnectValue(value => this.ShowReplayGain = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_LOCATION
                ).ConnectValue(value => this.ShowLocation = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SelectionPropertiesConfiguration.SECTION,
                    SelectionPropertiesConfiguration.SHOW_IMAGES
                ).ConnectValue(value => this.ShowImages = value);
            }
            base.OnConfigurationChanged();
        }

        protected virtual void OnSelectedItemChanged(object sender, EventArgs e)
        {
            var libraryHierarchyNode = this.LibraryManager.SelectedItem;
            if (libraryHierarchyNode == null)
            {
                return;
            }
            if (object.ReferenceEquals(this.FileDatasSource, libraryHierarchyNode))
            {
                return;
            }
            this.Dispatch(() => this.Refresh(libraryHierarchyNode));
        }

        protected virtual void OnSelectedPlaylistChanged(object sender, EventArgs e)
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null)
            {
                return;
            }
            if (object.ReferenceEquals(this.FileDatasSource, playlist))
            {
                return;
            }
            this.Dispatch(() => this.Refresh(playlist));
        }

        protected virtual void OnSelectedItemsChanged(object sender, EventArgs e)
        {
            var playlistItems = this.PlaylistManager.SelectedItems;
            if (playlistItems == null || !playlistItems.Any())
            {
                return;
            }
            if (object.ReferenceEquals(this.FileDatasSource, playlistItems))
            {
                return;
            }
            this.FileDatasSource = playlistItems;
            this.Dispatch(() => this.Refresh(playlistItems));
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return;
            }
            var playlistItems = new[] { outputStream.PlaylistItem };
            if (object.ReferenceEquals(this.FileDatasSource, playlistItems))
            {
                return;
            }
            this.FileDatasSource = playlistItems;
            this.Dispatch(() => this.Refresh(playlistItems));
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
                    break;
                case CommonSignals.PlaylistSelected:
                    this.OnPlaylistSelected();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null)
            {
                var playlist = state.Playlists.FirstOrDefault();
                if (object.ReferenceEquals(this.FileDatasSource, playlist))
                {
                    this.Dispatch(() => this.Refresh(playlist));
                }
            }
            else
            {
                //Nothing to do.
            }
        }

        protected virtual void OnPlaylistSelected()
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (object.ReferenceEquals(this.FileDatasSource, playlist))
            {
                return;
            }
            this.Dispatch(() => this.Refresh(playlist));
        }

        protected virtual void Refresh()
        {
            if (this.FileDatas == null)
            {
                return;
            }
            this.Dispatch(() => this.Refresh(this.FileDatas.ToArray()));
        }

        protected virtual async Task Refresh(LibraryHierarchyNode libraryHierarchyNode)
        {
            await Windows.Invoke(() =>
            {
                this.StatusMessage = Strings.SelectionProperties_Loading;
                this.HasStatusMessage = true;
            }).ConfigureAwait(false);
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(libraryHierarchyNode);
            if (libraryItems == null || !libraryItems.Any())
            {
                return;
            }
            this.FileDatasSource = libraryHierarchyNode;
            await this.Refresh(libraryItems).ConfigureAwait(false);
        }

        protected virtual Task Refresh(Playlist playlist)
        {
            var playlistItems = this.PlaylistBrowser.GetItems(playlist);
            if (playlistItems == null || !playlistItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            this.FileDatasSource = playlist;
            return this.Refresh(playlistItems);
        }

        protected virtual Task Refresh(IEnumerable<IFileData> fileDatas)
        {
            var metaDatas = this.GetMetaDatas(fileDatas);
            var tags = this.GetTags(fileDatas, metaDatas).ToArray();
            var properties = this.GetProperties(fileDatas, metaDatas).ToArray();
            var replaygain = this.GetReplayGain(fileDatas, metaDatas).ToArray();
            var filesystem = this.GetFileSystem(fileDatas, metaDatas).ToArray();
            var images = this.GetImages(fileDatas, metaDatas).ToArray();
            //Forking so we don't block the UI.
            this.Debouncer.Exec(() =>
            {
                var task = Windows.Invoke(() =>
                {
                    this.StatusMessage = string.Empty;
                    this.HasStatusMessage = false;
                    this.FileDatas.ResetSync(fileDatas);
                    this.Tags.ResetSync(tags);
                    this.Properties.ResetSync(properties);
                    this.ReplayGain.ResetSync(replaygain);
                    this.FileSystem.ResetSync(filesystem);
                    this.Images.ResetSync(images);
                });
            });
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual IDictionary<int, IDictionary<string, string>> GetMetaDatas(IEnumerable<IFileData> fileDatas)
        {
            return fileDatas.ToDictionary(
                fileData => fileData.Id,
                fileData => this.GetMetaData(fileData)
            );
        }

        protected virtual IDictionary<string, string> GetMetaData(IFileData fileData)
        {
            if (fileData.MetaDatas == null)
            {
                return null;
            }
            lock (fileData.MetaDatas)
            {
                return fileData.MetaDatas.ToDictionary2();
            }
        }

        protected virtual IEnumerable<Row> GetTags(IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            if (this.ShowTags)
            {
                foreach (var tag in TAGS)
                {
                    yield return this.GetRow(tag, fileDatas, metaDatas);
                }
            }
        }

        protected virtual IEnumerable<Row> GetProperties(IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            if (this.ShowProperties)
            {
                foreach (var property in PROPERTIES)
                {
                    yield return this.GetRow(property, fileDatas, metaDatas);
                }
            }
        }

        protected virtual IEnumerable<Row> GetReplayGain(IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            if (this.ShowReplayGain)
            {
                foreach (var replaygain in REPLAYGAIN)
                {
                    yield return this.GetRow(replaygain, fileDatas, metaDatas);
                }
            }
        }

        protected virtual IEnumerable<Row> GetFileSystem(IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            if (this.ShowLocation)
            {
                foreach (var property in FILESYSTEM)
                {
                    yield return this.GetRow(property, fileDatas, metaDatas);
                }
            }
        }

        protected virtual IEnumerable<Row> GetImages(IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            const int LIMIT = 10;
            if (this.ShowImages)
            {
                foreach (var image in IMAGES)
                {
                    foreach (var row in this.GetRows(image, fileDatas, metaDatas, LIMIT))
                    {
                        yield return row;
                    }
                }
            }
        }

        protected virtual Row GetRow(string name, IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas)
        {
            var values = new List<object>();
            var provider = this.GetProvider(name);
            var aggregator = this.GetAggregator(name);
            foreach (var fileData in fileDatas)
            {
                var metaData = default(IDictionary<string, string>);
                if (!metaDatas.TryGetValue(fileData.Id, out metaData))
                {
                    metaData = null;
                }
                try
                {
                    aggregator.Add(
                        fileData,
                        values,
                        provider.GetValue(fileData, metaData, name)
                    );
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to read property \"{0}\" of file \"{1}\": {2}", name, fileData.FileName, e.Message);
                }
            }
            var value = aggregator.GetValue(values);
            return new Row(name, value);
        }

        protected virtual IEnumerable<Row> GetRows(string name, IEnumerable<IFileData> fileDatas, IDictionary<int, IDictionary<string, string>> metaDatas, int limit)
        {
            var values = new List<string>();
            var provider = this.GetProvider(name);
            foreach (var fileData in fileDatas)
            {
                var metaData = default(IDictionary<string, string>);
                if (!metaDatas.TryGetValue(fileData.Id, out metaData))
                {
                    continue;
                }
                var value = Convert.ToString(provider.GetValue(fileData, metaData, name));
                if (string.IsNullOrEmpty(value) || values.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                values.Add(value);
                if (values.Count >= limit)
                {
                    break;
                }
            }
            foreach (var value in values)
            {
                yield return new Row(name, value);
            }
        }

        protected virtual ValueProvider GetProvider(string name)
        {
            var provider = default(ValueProvider);
            if (PROVIDERS.TryGetValue(name, out provider))
            {
                return provider;
            }
            return TextMetaDataValueProvider.Instance;
        }

        protected virtual ValueAggregator GetAggregator(string name)
        {
            var aggregator = default(ValueAggregator);
            if (AGGREGATORS.TryGetValue(name, out aggregator))
            {
                return aggregator;
            }
            return ConcatValueAggregator.Instance;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new SelectionProperties();
        }

        protected override void OnDisposing()
        {
            if (this.LibraryManager != null)
            {
                this.LibraryManager.SelectedItemChanged -= this.OnSelectedItemChanged;
            }
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.SelectedPlaylistChanged -= this.OnSelectedPlaylistChanged;
                this.PlaylistManager.SelectedItemsChanged -= this.OnSelectedItemsChanged;
            }
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }

        public abstract class ValueProvider
        {
            public abstract object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name);
        }

        public class MetaDataValueProvider : ValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = default(string);
                if (metaData != null && metaData.TryGetValue(name, out value))
                {
                    return value;
                }
                return null;
            }

            public static readonly ValueProvider Instance = new MetaDataValueProvider();
        }

        public class TextMetaDataValueProvider : MetaDataValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = default(string);
                if (metaData != null && metaData.TryGetValue(name, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
                return Strings.SelectionProperties_NoValue;
            }

            new public static readonly ValueProvider Instance = new TextMetaDataValueProvider();
        }

        public class BooleanMetaDataValueProvider : MetaDataValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = default(string);
                if (metaData != null && metaData.TryGetValue(name, out value) && !string.IsNullOrEmpty(value))
                {
                    var boolean = default(bool);
                    if (bool.TryParse(value, out boolean))
                    {
                        if (boolean)
                        {
                            return Strings.SelectionProperties_True;
                        }
                    }
                }
                return Strings.SelectionProperties_False;
            }

            new public static readonly ValueProvider Instance = new BooleanMetaDataValueProvider();
        }

        public class NumericMetaDataValueProvider : MetaDataValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = default(string);
                if (metaData != null && metaData.TryGetValue(name, out value) && !string.IsNullOrEmpty(value))
                {
                    var numeric = default(long);
                    if (long.TryParse(value, out numeric))
                    {
                        return numeric;
                    }
                }
                return 0;
            }

            new public static readonly ValueProvider Instance = new NumericMetaDataValueProvider();
        }

        public class CountValueProvider : ValueProvider
        {
            public const string NAME = "SelectionCount";

            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                return name;
            }

            public static readonly ValueProvider Instance = new CountValueProvider();
        }

        public class BitrateValueProvider : ValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = Convert.ToInt32(NumericMetaDataValueProvider.Instance.GetValue(fileData, metaData, name));
                if (value == 0)
                {
                    var sampleRate = Convert.ToInt32(NumericMetaDataValueProvider.Instance.GetValue(fileData, metaData, CommonProperties.AudioSampleRate));
                    var bitPerSample = Convert.ToInt32(NumericMetaDataValueProvider.Instance.GetValue(fileData, metaData, CommonProperties.BitsPerSample));
                    var channels = Convert.ToInt32(NumericMetaDataValueProvider.Instance.GetValue(fileData, metaData, CommonProperties.AudioChannels));
                    if (sampleRate != 0 && bitPerSample != 0 && channels != 0)
                    {
                        value = (sampleRate * bitPerSample * channels) / 1000;
                    }
                    else
                    {
                        return Strings.SelectionProperties_NoValue;
                    }
                }
                return MetaDataInfo.BitRateDescription(value);
            }

            public static readonly ValueProvider Instance = new BitrateValueProvider();
        }

        public class SamplerateValueProvider : ValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = Convert.ToInt32(NumericMetaDataValueProvider.Instance.GetValue(fileData, metaData, name));
                if (value == 0)
                {
                    return Strings.SelectionProperties_NoValue;
                }
                return MetaDataInfo.SampleRateDescription(value);
            }

            public static readonly ValueProvider Instance = new SamplerateValueProvider();
        }

        public class DecibelValueProvider : ValueProvider
        {
            public override object GetValue(IFileData fileData, IDictionary<string, string> metaData, string name)
            {
                var value = Convert.ToString(MetaDataValueProvider.Instance.GetValue(fileData, metaData, name));
                if (string.IsNullOrEmpty(value))
                {
                    return Strings.SelectionProperties_NoValue;
                }
                var numeric = default(float);
                if (!float.TryParse(value, out numeric))
                {
                    return Strings.SelectionProperties_NoValue;
                }
                if (float.IsNaN(numeric))
                {
                    return Strings.SelectionProperties_NoValue;
                }
                return string.Format("{0}{1:0.00}dB", numeric > 0 ? "+" : string.Empty, numeric);
            }

            public static readonly ValueProvider Instance = new DecibelValueProvider();
        }

        public abstract class ValueAggregator
        {
            public abstract void Add(IFileData fileData, IList<object> values, object value);

            public abstract string GetValue(IEnumerable<object> values);
        }

        public class ConcatValueAggregator : ValueAggregator
        {
            const int LIMIT = 30;

            const string DELIMITER = "; ";

            const string OVERFLOW = "...";

            public static readonly ConditionalWeakTable<IList<object>, ISet<string>> History = new ConditionalWeakTable<IList<object>, ISet<string>>();

            public override void Add(IFileData fileData, IList<object> values, object value)
            {
                if (values.Count > LIMIT)
                {
                    return;
                }
                else if (values.Count == LIMIT)
                {
                    values.Add(OVERFLOW);
                    return;
                }
                var history = default(ISet<string>);
                if (!History.TryGetValue(values, out history))
                {
                    history = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    History.Add(values, history);
                }
                if (!history.Add(Convert.ToString(value)))
                {
                    return;
                }
                values.Add(value);
            }

            public override string GetValue(IEnumerable<object> values)
            {
                if (!values.Any())
                {
                    return Strings.SelectionProperties_NoValue;
                }
                return string.Join(
                    DELIMITER,
                    values
                );
            }

            public static readonly ValueAggregator Instance = new ConcatValueAggregator();
        }

        public class TimeSpanValueAggregator : ValueAggregator
        {
            public override void Add(IFileData fileData, IList<object> values, object value)
            {
                if (value is string text)
                {
                    var numeric = default(long);
                    if (!long.TryParse(text, out numeric))
                    {
                        return;
                    }
                }
                values.Add(value);
            }

            public override string GetValue(IEnumerable<object> values)
            {
                if (!values.Any())
                {
                    return Strings.SelectionProperties_NoValue;
                }
                var total = values.Select(value => Convert.ToInt64(value) / 1000).Sum();
                if (total == 0)
                {
                    return Strings.SelectionProperties_NoValue;
                }
                return TimeSpan.FromSeconds(total).ToString();
            }

            public static readonly ValueAggregator Instance = new TimeSpanValueAggregator();
        }

        public class FileSystemUsageValueAggregator : ValueAggregator
        {
            private static readonly string[] SUFFIX = { "B", "KB", "MB", "GB", "TB" };

            public override void Add(IFileData fileData, IList<object> values, object value)
            {
                if (value is string text)
                {
                    var numeric = default(long);
                    if (!long.TryParse(text, out numeric))
                    {
                        return;
                    }
                }
                values.Add(value);
            }

            public override string GetValue(IEnumerable<object> values)
            {
                if (!values.Any())
                {
                    return Strings.SelectionProperties_NoValue;
                }
                var total = values.Select(value => Convert.ToInt64(value)).Sum();
                if (total == 0)
                {
                    return Strings.SelectionProperties_NoValue;
                }
                var length = total;
                var order = 0;
                while (length >= 1024 && order < SUFFIX.Length - 1)
                {
                    order++;
                    length = length / 1024;
                }
                return string.Format("{0:0.##} {1} ({2} bytes)", length, SUFFIX[order], total);
            }

            public static readonly ValueAggregator Instance = new FileSystemUsageValueAggregator();
        }

        public class CountValueAggregator : ValueAggregator
        {
            public const string NAME = "SelectionCount";

            public override void Add(IFileData fileData, IList<object> values, object value)
            {
                values.Add(value);
            }

            public override string GetValue(IEnumerable<object> values)
            {
                var count = values.Count();
                return Convert.ToString(count);
            }

            public static readonly ValueAggregator Instance = new CountValueAggregator();
        }

        public class Row
        {
            public Row(string name, string value)
            {
                this.Name = name;
                this.Value = value;
            }

            public string Name { get; private set; }

            public string Value { get; private set; }
        }
    }
}
