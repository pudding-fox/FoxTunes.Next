using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes.ViewModel
{
    public abstract class PlaylistBase : ViewModelBase
    {
        protected virtual int MaxItems
        {
            get
            {
                return -1;
            }
        }

        protected virtual string LOADING
        {
            get
            {
                return Strings.PlaylistBase_Loading;
            }
        }

        protected virtual string UPDATING
        {
            get
            {
                return Strings.PlaylistBase_Updating;
            }
        }

        protected virtual string EMPTY
        {
            get
            {
                return Strings.PlaylistBase_Empty;
            }
        }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileActionHandlerManager FileActionHandlerManager { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        private PlaylistItem[] _Items { get; set; }

        public PlaylistItem[] Items
        {
            get
            {
                return this._Items;
            }
            set
            {
                this._Items = value;
                this.OnItemsChanged();
            }
        }

        protected virtual void OnItemsChanged()
        {
            if (this.ItemsChanged != null)
            {
                this.ItemsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Items");
        }

        public event EventHandler ItemsChanged;

        protected virtual string GetStatusMessage()
        {
            if (this.PlaylistBrowser == null || this.PlaylistManager == null || this.Items == null)
            {
                return LOADING;
            }
            if (this.Items.Length > 0)
            {
                return null;
            }
            switch (this.PlaylistBrowser.State)
            {
                case PlaylistBrowserState.Loading:
                    return LOADING;
            }
            var playlist = this.GetPlaylist();
            var isUpdating = global::FoxTunes.BackgroundTask.Active
                    .OfType<PlaylistTaskBase>()
                    .Any(task => task.Playlist == playlist);
            if (isUpdating)
            {
                return UPDATING;
            }
            else
            {
                return EMPTY;
            }
        }

        private string _StatusMessage { get; set; }

        public virtual string StatusMessage
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

        public virtual bool HasStatusMessage
        {
            get
            {
                if (this.Items == null || this.Items.Length == 0)
                {
                    return true;
                }
                return false;
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

        protected virtual async Task RefreshStatus()
        {
            var statusMessage = this.GetStatusMessage();
            await Windows.Invoke(() =>
            {
                this.StatusMessage = statusMessage;
                this.OnHasStatusMessageChanged();
            }).ConfigureAwait(false);
        }

        public virtual Task Refresh()
        {
            return this.RefreshItems();
        }

        protected abstract Playlist GetPlaylist();

        protected override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaylistBrowser.StateChanged += this.OnStateChanged;
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            this.FileActionHandlerManager = core.Managers.FileActionHandler;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            //TODO: Bad .Wait().
            this.RefreshStatus().Wait();
            base.InitializeComponent(core);
        }

        protected virtual void OnActiveChanged(object sender, EventArgs e)
        {
            var task = this.RefreshStatus();
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    return this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null && state.Playlists.Any())
            {
                var playlist = this.GetPlaylist();
                if (playlist == null || state.Playlists.Contains(playlist))
                {
                    await this.Refresh().ConfigureAwait(false);
                }
            }
            else
            {
                await this.Refresh().ConfigureAwait(false);
            }
        }

        protected virtual void OnStateChanged(object sender, EventArgs e)
        {
            var task = this.RefreshStatus();
        }

        protected virtual async Task RefreshItems()
        {
            var playlist = this.GetPlaylist();
            await this.RefreshItems(playlist).ConfigureAwait(false);
        }

        protected virtual async Task RefreshItems(Playlist playlist)
        {
            if (playlist == null)
            {
                return;
            }
            var items = this.PlaylistBrowser.GetItems(playlist);
            if (this.MaxItems > 0 && items.Length > this.MaxItems)
            {
                Logger.Write(this, LogLevel.Warn, "Max items for playlist type {0} exceeded, results will be limited.", this.GetType().Name);
                items = items.Take(this.MaxItems).ToArray();
            }
            await Windows.Invoke(() => this.Items = items).ConfigureAwait(false);
            await this.RefreshStatus().ConfigureAwait(false);
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            if (this.PlaylistBrowser != null)
            {
                this.PlaylistBrowser.StateChanged -= this.OnStateChanged;
            }
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }
    }
}
