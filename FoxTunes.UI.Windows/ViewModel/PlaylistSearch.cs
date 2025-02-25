using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class PlaylistSearch : ViewModelBase
    {
        public string Filter
        {
            get
            {
                if (this.PlaylistManager == null)
                {
                    return null;
                }
                return this.PlaylistManager.Filter;
            }
            set
            {
                if (this.PlaylistManager == null)
                {
                    return;
                }
                this.PlaylistManager.Filter = value;
            }
        }

        protected virtual void OnFilterChanged()
        {
            if (this.FilterChanged != null)
            {
                this.FilterChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Filter");
        }

        public event EventHandler FilterChanged;

        private int _SearchInterval { get; set; }

        public int SearchInterval
        {
            get
            {
                return this._SearchInterval;
            }
            set
            {
                this._SearchInterval = value;
                this.OnSearchIntervalChanged();
            }
        }

        protected virtual void OnSearchIntervalChanged()
        {
            if (this.SearchIntervalChanged != null)
            {
                this.SearchIntervalChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SearchInterval");
        }

        public event EventHandler SearchIntervalChanged;

        public IPlaylistManager PlaylistManager { get; private set; }

        private IConfiguration Configuration { get; set; }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.FilterChanged += this.OnFilterChanged;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<IntegerConfigurationElement>(
                SearchBehaviourConfiguration.SECTION,
                SearchBehaviourConfiguration.SEARCH_INTERVAL_ELEMENT
            ).ConnectValue(value => this.SearchInterval = value);
            base.InitializeComponent(core);
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnFilterChanged);
        }

        protected override void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.FilterChanged -= this.OnFilterChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LibrarySearch();
        }
    }
}
