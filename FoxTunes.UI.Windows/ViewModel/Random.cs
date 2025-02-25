using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Random : ViewModelBase
    {
        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaylistManager = core.Managers.Playlist;
            base.InitializeComponent(core);
        }

        public ICommand NextCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Next);
            }
        }

        public Task Next()
        {
            var random = new global::System.Random(unchecked((int)DateTime.Now.Ticks));
            var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            var playlistItems = this.PlaylistBrowser.GetItems(playlist);
            var index = random.Next(playlistItems.Length);
            var playlistItem = playlistItems[index];
            return this.PlaylistManager.Play(playlistItem);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Random();
        }
    }
}
