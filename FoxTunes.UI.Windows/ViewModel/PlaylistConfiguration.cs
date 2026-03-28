using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class PlaylistConfiguration : ViewModelBase
    {
        private Playlist _Playlist { get; set; }

        public Playlist Playlist
        {
            get
            {
                return this._Playlist;
            }
            set
            {
                this._Playlist = value;
                this.OnPlaylistChanged();
            }
        }

        protected virtual void OnPlaylistChanged()
        {
            if (this.PlaylistChanged != null)
            {
                this.PlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Playlist");
        }

        public event EventHandler PlaylistChanged;

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

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            var task = this.Refresh();
            base.InitializeComponent(core);
        }

        private void OnSelectedPlaylistChanged(object sender, EventArgs e)
        {
            var task = this.Refresh();
        }

        public Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.Playlist = this.PlaylistManager.SelectedPlaylist;
                if (this.Playlist != null)
                {
                    switch (this.Playlist.Type)
                    {
                        case PlaylistType.Dynamic:
                        case PlaylistType.Smart:
                        case PlaylistType.AIPrompt:
                        case PlaylistType.AIDJ:
                            this.HasData = true;
                            break;
                        default:
                            this.HasData = false;
                            break;
                    }
                }
                else
                {
                    this.HasData = false;
                }
            });
        }

        public ICommand SaveCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Save);
            }
        }

        public async Task Save()
        {
            var exception = default(Exception);
            try
            {
                using (var database = this.DatabaseFactory.Create())
                {
                    using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var playlists = database.Set<Playlist>(transaction);
                            await playlists.AddOrUpdateAsync(this.Playlist).ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }))
                    {
                        await task.Run().ConfigureAwait(false);
                    }
                }
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistConfigUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.ErrorEmitter.Send(this, "Save", exception).ConfigureAwait(false);
            throw exception;
        }

        protected override void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.SelectedPlaylistChanged -= this.OnSelectedPlaylistChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaylistConfiguration();
        }
    }
}
