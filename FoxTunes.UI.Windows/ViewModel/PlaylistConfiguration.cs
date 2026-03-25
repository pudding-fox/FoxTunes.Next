using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class PlaylistConfiguration : ViewModelBase
    {
        private Playlist _SelectedPlaylist { get; set; }

        public Playlist SelectedPlaylist
        {
            get
            {
                return this._SelectedPlaylist;
            }
            set
            {
                if (object.ReferenceEquals(this._SelectedPlaylist, value))
                {
                    return;
                }
                this._SelectedPlaylist = value;
                this.OnSelectedPlaylistChanged();
            }
        }

        protected virtual void OnSelectedPlaylistChanged()
        {
            if (this.SelectedPlaylistChanged != null)
            {
                this.SelectedPlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedPlaylist");
        }

        public event EventHandler SelectedPlaylistChanged;

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

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
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
                this.SelectedPlaylist = this.PlaylistManager.SelectedPlaylist;
                if (this.SelectedPlaylist != null && !string.IsNullOrEmpty(this.SelectedPlaylist.Config))
                {
                    this.HasData = true;
                }
                else
                {
                    this.HasData = false;
                }
            });
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
