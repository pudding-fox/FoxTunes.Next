using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

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

        public IPlaylistManager PlaylistManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.SignalEmitter = core.Components.SignalEmitter;
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
                if (this.Playlist != null)
                {
                    this.Playlist.ConfigChanged -= this.OnConfigChanged;
                }
                this.Playlist = this.PlaylistManager.SelectedPlaylist;
                if (this.Playlist != null)
                {
                    this.Playlist.ConfigChanged += this.OnConfigChanged;
                    if (string.IsNullOrEmpty(this.Playlist.Config))
                    {
                        this.HasData = false;
                    }
                    else
                    {
                        this.HasData = true;
                    }
                }
                else
                {
                    this.HasData = false;
                }
            });
        }

        protected virtual void OnConfigChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.Playlist.Config))
            {
                this.HasData = false;
            }
            else
            {
                this.HasData = true;
            }
            this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated)));
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
