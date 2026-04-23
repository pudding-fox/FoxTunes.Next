using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Lyrics : ViewModelBase
    {
        public static readonly Regex SYNCED_TAG = new Regex(@"[\w+:\w+]", RegexOptions.Compiled);

        public static readonly Regex SYNCED_LYRICS = new Regex(@"\[(\d{2}:\d{2}\.\d{2})\]\s+(.+)", RegexOptions.Compiled);

        public static readonly string PADDING = string.Join(string.Empty, Enumerable.Repeat(Environment.NewLine, 10));

        public IPlaybackManager PlaybackManager { get; private set; }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private bool _HasData { get; set; }

        public bool HasData
        {
            get
            {
                return this._HasData;
            }
            set
            {
                this._HasData = value;
                this.OnHasDataChanged();
            }
        }

        protected virtual void OnHasDataChanged()
        {
            if (this.HasDataChanged != null)
            {
                this.HasDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasData");
        }

        public event EventHandler HasDataChanged;

        private bool _HasPlainData { get; set; }

        public bool HasPlainData
        {
            get
            {
                return this._HasPlainData;
            }
            set
            {
                this._HasPlainData = value;
                this.OnHasPlainDataChanged();
            }
        }

        protected virtual void OnHasPlainDataChanged()
        {
            if (this.HasPlainDataChanged != null)
            {
                this.HasPlainDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasPlainData");
        }

        public event EventHandler HasPlainDataChanged;

        private bool _HasSyncedData { get; set; }

        public bool HasSyncedData
        {
            get
            {
                return this._HasSyncedData;
            }
            set
            {
                this._HasSyncedData = value;
                this.OnHasSyncedDataChanged();
            }
        }

        protected virtual void OnHasSyncedDataChanged()
        {
            if (this.HasSyncedDataChanged != null)
            {
                this.HasSyncedDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasSyncedData");
        }

        public event EventHandler HasSyncedDataChanged;

        private bool _AutoScroll { get; set; }

        public bool AutoScroll
        {
            get
            {
                return this._AutoScroll;
            }
            set
            {
                this._AutoScroll = value;
                this.OnAutoScrollChanged();
            }
        }

        protected virtual void OnAutoScrollChanged()
        {
            PlaybackStateNotifier.Notify -= this.OnNotify;
            if (this.AutoScroll)
            {
                PlaybackStateNotifier.Notify += this.OnNotify;
            }
            if (this.AutoScrollChanged != null)
            {
                this.AutoScrollChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AutoScroll");
        }

        public event EventHandler AutoScrollChanged;

        private string _PlainData { get; set; }

        public string PlainData
        {
            get
            {
                return this._PlainData;
            }
            set
            {
                this._PlainData = value;
                this.OnPlainDataChanged();
            }
        }

        protected virtual void OnPlainDataChanged()
        {
            if (this.PlainDataChanged != null)
            {
                this.PlainDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("PlainData");
        }

        public event EventHandler PlainDataChanged;

        private SyncedLyrics[] _SyncedData { get; set; }

        public SyncedLyrics[] SyncedData
        {
            get
            {
                return this._SyncedData;
            }
            set
            {
                this._SyncedData = value;
                this.OnSyncedDataChanged();
            }
        }

        protected virtual void OnSyncedDataChanged()
        {
            if (this.SyncedDataChanged != null)
            {
                this.SyncedDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SyncedData");
        }

        public event EventHandler SyncedDataChanged;

        protected virtual void OnIsSyncedChanged()
        {
            if (this.IsSyncedChanged != null)
            {
                this.IsSyncedChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSynced");
        }

        public event EventHandler IsSyncedChanged;

        public long Position
        {
            get
            {
                if (!this.AutoScroll)
                {
                    return 0;
                }
                if (this.PlaybackManager == null)
                {
                    return 0;
                }
                var outputStream = this.PlaybackManager.CurrentStream;
                if (outputStream == null)
                {
                    return 0;
                }
                return outputStream.Position;
            }
        }

        protected virtual void OnPositionChanged()
        {
            if (this.PositionChanged != null)
            {
                this.PositionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Position");
        }

        public event EventHandler PositionChanged;

        public long Length
        {
            get
            {
                if (!this.AutoScroll)
                {
                    return 0;
                }
                if (this.PlaybackManager == null)
                {
                    return 0;
                }
                var outputStream = this.PlaybackManager.CurrentStream;
                if (outputStream == null)
                {
                    return 0;
                }
                return outputStream.Length;
            }
        }

        protected virtual void OnLengthChanged()
        {
            if (this.LengthChanged != null)
            {
                this.LengthChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Length");
        }

        public event EventHandler LengthChanged;

        public SyncedLyrics CurrentLyrics
        {
            get
            {
                if (this.SyncedData == null)
                {
                    return null;
                }
                if (this.PlaybackManager == null)
                {
                    return null;
                }
                var outputStream = this.PlaybackManager.CurrentStream;
                if (outputStream == null)
                {
                    return null;
                }
                var duration = outputStream.GetDuration(this.Position);
                return this.SyncedData.LastOrDefault(data =>
                {
                    var time = TimeSpan.FromSeconds((data.Minute * 60) + data.Second);
                    return duration > time;
                });
            }
        }

        protected virtual void OnCurrentLyricsChanged()
        {
            if (this.CurrentLyricsChanged != null)
            {
                this.CurrentLyricsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentLyrics");
        }

        public event EventHandler CurrentLyricsChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.OnDemandMetaDataProvider = core.Components.OnDemandMetaDataProvider;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_LYRICS_TAGS
            ).ConnectValue(value =>
            {
                if (value)
                {
                    this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
                    this.Dispatch(this.Refresh);
                }
                else
                {
                    this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
                    if (this.HasPlainData || this.HasSyncedData)
                    {
                        var task = Windows.Invoke(() =>
                        {
                            this.HasData = false;
                            this.HasPlainData = false;
                            this.HasSyncedData = false;
                            this.PlainData = null;
                            this.SyncedData = null;
                        });
                    }
                }
            });
            this.Configuration.GetElement<BooleanConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.AUTO_SCROLL
            ).ConnectValue(value => this.AutoScroll = value);
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.MetaDataUpdated:
                    return this.OnMetaDataUpdated(signal.State as MetaDataUpdatedSignalState);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task OnMetaDataUpdated(MetaDataUpdatedSignalState state)
        {
            if (state != null && state.Names != null)
            {
                return this.Refresh(state.Names);
            }
            else
            {
                return this.Refresh(Enumerable.Empty<string>());
            }
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnNotify(object sender, EventArgs e)
        {
            if (this.HasPlainData)
            {
                this.OnPositionChanged();
            }
            else if (this.HasSyncedData)
            {
                this.OnCurrentLyricsChanged();
            }
        }

        protected virtual Task Refresh(IEnumerable<string> names)
        {
            if (names != null && names.Any())
            {
                if (!names.Contains(CommonMetaData.Lyrics, StringComparer.OrdinalIgnoreCase))
                {
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }
            }
            return this.Refresh();
        }

        protected virtual async Task Refresh()
        {
            var data = default(string);
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream != null)
            {
                data = await this.OnDemandMetaDataProvider.GetMetaData(
                    outputStream.PlaylistItem,
                    new OnDemandMetaDataRequest(
                        CommonMetaData.Lyrics,
                        MetaDataItemType.Tag,
                        MetaDataUpdateType.System
                    )
                ).ConfigureAwait(false);
            }
            await Windows.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(data))
                {
                    var syncedData = new List<SyncedLyrics>();
                    using (var reader = new StringReader(data))
                    {
                        var line = default(string);
                        while ((line = reader.ReadLine()) != null)
                        {
                            {
                                var match = SYNCED_LYRICS.Match(line);
                                if (match.Success)
                                {
                                    syncedData.Add(new SyncedLyrics(match.Groups[1].Value, match.Groups[2].Value));
                                    continue;
                                }
                            }
                            {
                                var match = SYNCED_TAG.Match(line);
                                if (match.Success)
                                {
                                    continue;
                                }
                                else
                                {
                                    break;
                                }
                            }

                        }
                    }
                    if (syncedData.Any())
                    {
                        this.HasData = true;
                        this.HasPlainData = false;
                        this.HasSyncedData = true;
                        this.SyncedData = syncedData.ToArray();
                    }
                    else
                    {
                        this.HasData = true;
                        this.HasPlainData = true;
                        this.HasSyncedData = false;
                        this.PlainData = data;
                    }
                }
                else
                {
                    this.HasData = false;
                    this.HasPlainData = false;
                    this.HasSyncedData = false;
                    this.PlainData = null;
                    this.SyncedData = null;
                }
                this.OnPositionChanged();
                this.OnLengthChanged();
            }).ConfigureAwait(false);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Lyrics();
        }

        protected override void OnDisposing()
        {
            PlaybackStateNotifier.Notify -= this.OnNotify;
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            base.OnDisposing();
        }
    }

    public class SyncedLyrics
    {
        public SyncedLyrics(string timeStamp, string data)
        {
            this.TimeStamp = timeStamp;
            this.Data = data;
        }

        public string TimeStamp { get; private set; }

        public int Minute
        {
            get
            {
                return Convert.ToInt32(this.TimeStamp.Split(':').First());
            }
        }

        public int Second
        {
            get
            {
                return Convert.ToInt32(this.TimeStamp.Split(':').Last().Split('.').First());
            }
        }

        public int Hundredth
        {
            get
            {
                return Convert.ToInt32(this.TimeStamp.Split(':').Last().Split('.').Last());
            }
        }

        public string Data { get; private set; }
    }
}
