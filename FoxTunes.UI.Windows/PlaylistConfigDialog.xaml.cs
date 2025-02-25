using FoxTunes.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for PlaylistConfigDialog.xaml
    /// </summary>
    public partial class PlaylistConfigDialog : UserControl
    {
        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            "Playlist",
            typeof(Playlist),
            typeof(PlaylistConfigDialog),
            new PropertyMetadata(new PropertyChangedCallback(OnPlaylistChanged))
        );

        public static Playlist GetPlaylist(PlaylistConfigDialog source)
        {
            return (Playlist)source.GetValue(PlaylistProperty);
        }

        public static void SetPlaylist(PlaylistConfigDialog source, Playlist value)
        {
            source.SetValue(PlaylistProperty, value);
        }

        private static void OnPlaylistChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var playlistConfigDialog = sender as PlaylistConfigDialog;
            if (playlistConfigDialog == null)
            {
                return;
            }
            playlistConfigDialog.OnPlaylistChanged();
        }

        public PlaylistConfigDialog()
        {
            this.InitializeComponent();
        }

        public Playlist Playlist
        {
            get
            {
                return this.GetValue(PlaylistProperty) as Playlist;
            }
            set
            {
                this.SetValue(PlaylistProperty, value);
            }
        }

        protected virtual void OnPlaylistChanged()
        {
            if (this.PlaylistChanged != null)
            {
                this.PlaylistChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler PlaylistChanged;
    }
}
