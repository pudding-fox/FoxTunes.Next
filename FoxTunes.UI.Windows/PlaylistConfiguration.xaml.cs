using System;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for PlaylistConfiguration.xaml
    /// </summary>
    [UIComponent(ID, role: UIComponentRole.Playlist)]
    public partial class PlaylistConfiguration : UIComponentBase
    {
        public const string ID = "44FC86FA-E45C-4C2A-A6EF-F2193494DF26";

        public PlaylistConfiguration()
        {
            this.InitializeComponent();
            var viewModel = default(global::FoxTunes.ViewModel.PlaylistConfiguration);
            if (this.TryFindResource<global::FoxTunes.ViewModel.PlaylistConfiguration>("ViewModel", out viewModel))
            {
                viewModel.PlaylistChanged += this.OnPlaylistChanged;
                this.OnPlaylistChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPlaylistChanged(object sender, EventArgs e)
        {
            var viewModel = default(global::FoxTunes.ViewModel.PlaylistConfiguration);
            if (this.TryFindResource<global::FoxTunes.ViewModel.PlaylistConfiguration>("ViewModel", out viewModel))
            {
                this.PlaylistConfigDialog.Playlist = viewModel.Playlist;
            }
        }
    }
}
