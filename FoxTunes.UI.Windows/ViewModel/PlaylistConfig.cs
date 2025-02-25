using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class PlaylistConfig : ViewModelBase
    {
        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            "Playlist",
            typeof(Playlist),
            typeof(PlaylistConfig),
            new PropertyMetadata(new PropertyChangedCallback(OnPlaylistChanged))
        );

        public static Playlist GetPlaylist(PlaylistConfig source)
        {
            return (Playlist)source.GetValue(PlaylistProperty);
        }

        public static void SetPlaylist(PlaylistConfig source, Playlist value)
        {
            source.SetValue(PlaylistProperty, value);
        }

        public static void OnPlaylistChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var playlistConfig = sender as PlaylistConfig;
            if (playlistConfig == null)
            {
                return;
            }
            playlistConfig.OnPlaylistChanged();
        }

        public PlaylistConfig()
        {

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
            this.Refresh();
            if (this.Playlist != null)
            {
                this.Playlist.TypeChanged -= this.OnTypeChanged;
                this.Playlist.TypeChanged += this.OnTypeChanged;
            }
            if (this.PlaylistChanged != null)
            {
                this.PlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Playlist");
        }

        public event EventHandler PlaylistChanged;

        private PlaylistConfigBase _Config { get; set; }

        public PlaylistConfigBase Config
        {
            get
            {
                return this._Config;
            }
            set
            {
                this._Config = value;
                this.OnConfigChanged();
            }
        }

        protected virtual void OnConfigChanged()
        {
            if (this.ConfigChanged != null)
            {
                this.ConfigChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Config");
        }

        public event EventHandler ConfigChanged;

        public void Refresh()
        {
            if (this.Playlist == null)
            {
                this.Config = null;
                return;
            }
            this.Config = PlaylistConfigFactory.Create(this.Playlist);
        }

        protected virtual void OnTypeChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaylistConfig();
        }

        protected override void OnDisposing()
        {
            if (this.Playlist != null)
            {
                this.Playlist.TypeChanged -= this.OnTypeChanged;
            }
            base.OnDisposing();
        }
    }
}
