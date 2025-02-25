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
            typeof(PlaylistConfigDialog)
        );

        public static Playlist GetPlaylist(PlaylistConfigDialog source)
        {
            return (Playlist)source.GetValue(PlaylistProperty);
        }

        public static void SetPlaylist(PlaylistConfigDialog source, Playlist value)
        {
            source.SetValue(PlaylistProperty, value);
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
    }
}
