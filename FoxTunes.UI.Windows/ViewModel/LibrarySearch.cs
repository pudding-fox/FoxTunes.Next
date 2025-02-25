using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class LibrarySearch : ViewModelBase
    {
        public string Filter
        {
            get
            {
                if (this.LibraryHierarchyBrowser == null)
                {
                    return null;
                }
                return this.LibraryHierarchyBrowser.Filter;
            }
            set
            {
                if (this.LibraryHierarchyBrowser == null)
                {
                    return;
                }
                this.LibraryHierarchyBrowser.Filter = value;
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

        private SearchCommitBehaviour _SearchCommitBehaviour { get; set; }

        public SearchCommitBehaviour SearchCommitBehaviour
        {
            get
            {
                return this._SearchCommitBehaviour;
            }
            set
            {
                this._SearchCommitBehaviour = value;
                this.OnSearchCommitBehaviourChanged();
            }
        }

        protected virtual void OnSearchCommitBehaviourChanged()
        {
            if (this.SearchCommitBehaviourChanged != null)
            {
                this.SearchCommitBehaviourChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SearchCommitBehaviour");
        }

        public event EventHandler SearchCommitBehaviourChanged;

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        private IConfiguration Configuration { get; set; }

        protected override void InitializeComponent(ICore core)
        {
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.LibraryHierarchyBrowser.FilterChanged += this.OnFilterChanged;
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<IntegerConfigurationElement>(
                SearchBehaviourConfiguration.SECTION,
                SearchBehaviourConfiguration.SEARCH_INTERVAL_ELEMENT
            ).ConnectValue(value => this.SearchInterval = value);
            this.Configuration.GetElement<SelectionConfigurationElement>(
               SearchBehaviourConfiguration.SECTION,
               SearchBehaviourConfiguration.SEARCH_COMMIT_ELEMENT
           ).ConnectValue(option => this.SearchCommitBehaviour = SearchBehaviourConfiguration.GetCommitBehaviour(option));
            base.InitializeComponent(core);
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnFilterChanged);
        }

        public ICommand SearchCommitCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.SearchCommit);
            }
        }

        public Task SearchCommit()
        {
            var libraryHierarchy = this.LibraryManager.SelectedHierarchy;
            if (libraryHierarchy == null || LibraryHierarchy.Empty.Equals(libraryHierarchy))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var clear = default(bool);
            switch (this.SearchCommitBehaviour)
            {
                case SearchCommitBehaviour.Replace:
                    clear = true;
                    break;
                case SearchCommitBehaviour.Append:
                    clear = false;
                    break;
            }
            return this.PlaylistManager.Add(
                this.PlaylistManager.SelectedPlaylist,
                this.LibraryHierarchyBrowser.GetNodes(libraryHierarchy),
                clear
            );
        }

        protected override void OnDisposing()
        {
            if (this.LibraryHierarchyBrowser != null)
            {
                this.LibraryHierarchyBrowser.FilterChanged -= this.OnFilterChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LibrarySearch();
        }
    }
}
