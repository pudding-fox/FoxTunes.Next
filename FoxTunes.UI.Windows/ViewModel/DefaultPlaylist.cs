using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class DefaultPlaylist : GridPlaylist
    {
        public Playlist CurrentPlaylist { get; private set; }

        protected override Playlist GetPlaylist()
        {
            return this.PlaylistManager.SelectedPlaylist;
        }

        protected override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.PlaylistManager.CurrentPlaylistChanged += this.OnCurrentPlaylistChanged;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnCurrentPlaylistChanged(object sender, EventArgs e)
        {
            var task = this.RefreshIfRequired();
        }

        protected virtual void OnSelectedPlaylistChanged(object sender, EventArgs e)
        {
            var task = this.RefreshIfRequired();
        }

        protected virtual Task RefreshIfRequired()
        {
            var playlist = this.GetPlaylist();
            if (object.ReferenceEquals(this.CurrentPlaylist, playlist))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Refresh();
        }

        public override Task Refresh()
        {
            this.CurrentPlaylist = this.GetPlaylist();
            return base.Refresh();
        }

        protected override void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.CurrentPlaylistChanged -= this.OnCurrentPlaylistChanged;
                this.PlaylistManager.SelectedPlaylistChanged -= this.OnSelectedPlaylistChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new DefaultPlaylist();
        }
    }
}
