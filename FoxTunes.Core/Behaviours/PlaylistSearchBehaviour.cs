using FoxTunes.Interfaces;
using System;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class PlaylistSearchBehaviour : StandardBehaviour
    {
        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.FilterChanged += this.OnFilterChanged;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            base.InitializeComponent(core);
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        {
            if (this.PlaylistManager.SelectedPlaylist == null || string.IsNullOrEmpty(this.PlaylistManager.Filter))
            {
                return;
            }
            this.Dispatch(this.Search);
        }

        public void Search()
        {
            var playlistItems = this.PlaylistBrowser.GetItems(this.PlaylistManager.SelectedPlaylist, this.PlaylistManager.Filter);
            if (playlistItems != null && playlistItems.Any())
            {
                this.PlaylistManager.SelectedItems = playlistItems;
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.FilterChanged -= this.OnFilterChanged;
            }
        }

        ~PlaylistSearchBehaviour()
        {
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
