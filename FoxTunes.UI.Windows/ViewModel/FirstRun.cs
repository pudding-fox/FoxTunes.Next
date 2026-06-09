using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class FirstRun : ViewModelBase
    {
        public PlaylistSettings PlaylistSettings { get; private set; }

        public IConfigurationBase Configuration { get; private set; }

        public BooleanConfigurationElement Radio { get; private set; }

        public SelectionConfigurationElement Theme { get; private set; }

        public SelectionConfigurationElement Layout { get; private set; }

        public IUILayoutProvider LayoutProvider
        {
            get
            {
                return LayoutManager.Instance.Provider;
            }
        }

        public BooleanConfigurationElement Transparency { get; private set; }

        public SelectionConfigurationElement TransparencyProvider { get; private set; }

        public BooleanConfigurationElement DiscogsEnabled { get; private set; }

        public BooleanConfigurationElement DiscogsAutoLookup { get; private set; }

        public BooleanConfigurationElement LyricsAutoLookup { get; private set; }

        public SelectionConfigurationElement Input { get; private set; }

        public SelectionConfigurationElement Output { get; private set; }

        public BooleanConfigurationElement Resampler { get; private set; }

        public BooleanConfigurationElement Memory { get; private set; }

        protected virtual void OnLayoutProviderChanged()
        {
            if (this.LayoutProviderChanged != null)
            {
                this.LayoutProviderChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("LayoutProvider");
        }

        public event EventHandler LayoutProviderChanged;

        protected override void InitializeComponent(ICore core)
        {
            LayoutManager.Instance.ProviderChanged += this.OnProviderChanged;
            this.PlaylistSettings = new PlaylistSettings();
            this.PlaylistSettings.PlaylistColumns.ItemsSourceChanged += this.OnItemsSourceChanged;
            this.Configuration = core.Components.Configuration;
            this.Radio = this.Configuration.GetElement<BooleanConfigurationElement>(
                "1D61F93D-EAA1-4D24-8418-BDB3FCE433C4",
                "AAAA4FF2-843E-4606-92B8-985611B2CC0A"
            );
            this.Theme = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.THEME
            );
            this.Layout = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.LAYOUT
            );
            this.Transparency = this.Configuration.GetElement<BooleanConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY
            );
            this.TransparencyProvider = this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER
            );
            this.DiscogsEnabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                "D41A188F-119B-4CBF-AF10-522A7AC77CAE",
                "AAAA76ED-AC82-4C9F-8FCE-096E3ABC2A47"
            );
            this.DiscogsAutoLookup = this.Configuration.GetElement<BooleanConfigurationElement>(
                "D41A188F-119B-4CBF-AF10-522A7AC77CAE",
                "FFGG9A7F-90EF-42D8-827F-5638A586B398"
            );
            this.LyricsAutoLookup = this.Configuration.GetElement<BooleanConfigurationElement>(
                "42FB4DBD-E28C-4E42-B64F-6921CFCEF924",
                "BBBB3698-E26A-4D6C-9BBF-E845B0F9D150"
            );
            this.Input = this.Configuration.GetElement<SelectionConfigurationElement>(
                "8399D051-81BC-41A6-940D-36E6764213D2",
                "MMNN52A1-79E9-43AA-BC17-C9DB335AFC9C"
            );
            this.Output = this.Configuration.GetElement<SelectionConfigurationElement>(
                "8399D051-81BC-41A6-940D-36E6764213D2",
                "NNNN6B39-2F8A-4667-9C03-9742FF2D1EA7"
            );
            this.Resampler = this.Configuration.GetElement<BooleanConfigurationElement>(
                "8399D051-81BC-41A6-940D-36E6764213D2",
                "AAAA5C85-178C-470D-A977-C54350875AB3"
            );
            this.Memory = this.Configuration.GetElement<BooleanConfigurationElement>(
                "8399D051-81BC-41A6-940D-36E6764213D2",
                "OOOOBED1-7965-47A3-9798-E46124386A8D"
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnProviderChanged(object sender, EventArgs e)
        {
            this.OnLayoutProviderChanged();
        }

        protected virtual void OnItemsSourceChanged(object sender, EventArgs e)
        {
            this.OnRatingsChanged();
            this.OnLikesChanged();
            this.OnMoodBarChanged();
        }

        public bool Ratings
        {
            get
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return false;
                }
                var ratings = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Rating", StringComparison.OrdinalIgnoreCase));
                if (ratings == null)
                {
                    return false;
                }
                return ratings.Enabled;
            }
            set
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return;
                }
                var ratings = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Rating", StringComparison.OrdinalIgnoreCase));
                if (ratings == null)
                {
                    return;
                }
                ratings.Enabled = value;
                var task = this.PlaylistSettings.Save();
                this.OnRatingsChanged();
            }
        }

        protected virtual void OnRatingsChanged()
        {
            if (this.RatingsChanged != null)
            {
                this.RatingsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Ratings");
        }

        public event EventHandler RatingsChanged;

        public bool Likes
        {
            get
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return false;
                }
                var like = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Like", StringComparison.OrdinalIgnoreCase));
                if (like == null)
                {
                    return false;
                }
                return like.Enabled;
            }
            set
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return;
                }
                var like = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Like", StringComparison.OrdinalIgnoreCase));
                if (like == null)
                {
                    return;
                }
                like.Enabled = value;
                var task = this.PlaylistSettings.Save();
                this.OnLikesChanged();
            }
        }

        protected virtual void OnLikesChanged()
        {
            if (this.LikesChanged != null)
            {
                this.LikesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Likes");
        }

        public event EventHandler LikesChanged;

        public bool MoodBar
        {
            get
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return false;
                }
                var moodBar = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Mood Bar", StringComparison.OrdinalIgnoreCase));
                if (moodBar == null)
                {
                    return false;
                }
                return moodBar.Enabled;
            }
            set
            {
                var playlistColumns = this.PlaylistSettings.PlaylistColumns.ItemsSource;
                if (playlistColumns == null)
                {
                    return;
                }
                var moodBar = playlistColumns.FirstOrDefault(column => string.Equals(column.Name, "Mood Bar", StringComparison.OrdinalIgnoreCase));
                if (moodBar == null)
                {
                    return;
                }
                moodBar.Enabled = value;
                var task = this.PlaylistSettings.Save();
                this.OnMoodBarChanged();
            }
        }

        protected virtual void OnMoodBarChanged()
        {
            if (this.MoodBarChanged != null)
            {
                this.MoodBarChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MoodBar");
        }

        public event EventHandler MoodBarChanged;

        public bool Discogs
        {
            get
            {
                if (this.DiscogsEnabled == null)
                {
                    return false;
                }
                if (this.DiscogsAutoLookup == null)
                {
                    return false;
                }
                return this.DiscogsEnabled.Value && this.DiscogsAutoLookup.Value;
            }
            set
            {
                if (this.DiscogsEnabled == null)
                {
                    return;
                }
                if (this.DiscogsAutoLookup == null)
                {
                    return;
                }
                this.DiscogsEnabled.Value = value;
                this.DiscogsAutoLookup.Value = value;
                this.OnDiscogsChanged();
            }
        }

        protected virtual void OnDiscogsChanged()
        {
            if (this.DiscogsChanged != null)
            {
                this.DiscogsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Discogs");
        }

        public event EventHandler DiscogsChanged;

        public bool Lyrics
        {
            get
            {
                if (this.LyricsAutoLookup == null)
                {
                    return false;
                }
                return this.LyricsAutoLookup.Value;
            }
            set
            {
                if (this.LyricsAutoLookup == null)
                {
                    return;
                }
                this.LyricsAutoLookup.Value = value;
                this.OnLyricsChanged();
            }
        }

        protected virtual void OnLyricsChanged()
        {
            if (this.LyricsChanged != null)
            {
                this.LyricsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Lyrics");
        }

        public event EventHandler LyricsChanged;

        protected override void OnDisposing()
        {
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.ProviderChanged -= this.OnProviderChanged;
            }
            if (this.PlaylistSettings != null)
            {
                this.PlaylistSettings.PlaylistColumns.ItemsSourceChanged -= this.OnItemsSourceChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new FirstRun();
        }
    }
}
