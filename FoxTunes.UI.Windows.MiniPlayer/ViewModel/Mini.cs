using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Mini : ViewModelBase
    {
        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileActionHandlerManager FileActionHandlerManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private BooleanConfigurationElement _Topmost { get; set; }

        public BooleanConfigurationElement Topmost
        {
            get
            {
                return this._Topmost;
            }
            set
            {
                this._Topmost = value;
                this.OnTopmostChanged();
            }
        }

        protected virtual void OnTopmostChanged()
        {
            if (this.TopmostChanged != null)
            {
                this.TopmostChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Topmost");
        }

        public event EventHandler TopmostChanged;

        private BooleanConfigurationElement _ShowArtwork { get; set; }

        public BooleanConfigurationElement ShowArtwork
        {
            get
            {
                return this._ShowArtwork;
            }
            set
            {
                this._ShowArtwork = value;
                this.OnShowArtworkChanged();
            }
        }

        protected virtual void OnShowArtworkChanged()
        {
            if (this.ShowArtworkChanged != null)
            {
                this.ShowArtworkChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowArtwork");
        }

        public event EventHandler ShowArtworkChanged;

        private BooleanConfigurationElement _ShowPlaylist { get; set; }

        public BooleanConfigurationElement ShowPlaylist
        {
            get
            {
                return this._ShowPlaylist;
            }
            set
            {
                this._ShowPlaylist = value;
                this.OnShowPlaylistChanged();
            }
        }

        protected virtual void OnShowPlaylistChanged()
        {
            if (this.ShowPlaylistChanged != null)
            {
                this.ShowPlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowPlaylist");
        }

        public event EventHandler ShowPlaylistChanged;

        private SelectionConfigurationElement _DropCommit { get; set; }

        public SelectionConfigurationElement DropCommit
        {
            get
            {
                return this._DropCommit;
            }
            set
            {
                this._DropCommit = value;
                this.OnDropCommitChanged();
            }
        }

        protected virtual void OnDropCommitChanged()
        {
            if (this.DropCommitChanged != null)
            {
                this.DropCommitChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DropCommit");
        }

        public event EventHandler DropCommitChanged;

        public ICommand DragEnterCommand
        {
            get
            {
                return new Command<DragEventArgs>(this.OnDragEnter);
            }
        }

        protected virtual void OnDragEnter(DragEventArgs e)
        {
            this.UpdateDragDropEffects(e);
        }

        public ICommand DragOverCommand
        {
            get
            {
                return new Command<DragEventArgs>(this.OnDragOver);
            }
        }

        protected virtual void OnDragOver(DragEventArgs e)
        {
            this.UpdateDragDropEffects(e);
        }

        protected virtual void UpdateDragDropEffects(DragEventArgs e)
        {
            var effects = DragDropEffects.None;
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    effects = DragDropEffects.Copy;
                }
                if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    effects = DragDropEffects.Copy;
                }
                if (ShellIDListHelper.GetDataPresent(e.Data))
                {
                    effects = DragDropEffects.Copy;
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to query clipboard contents: {0}", exception.Message);
            }
            e.Effects = effects;
        }

        public ICommand DropCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<DragEventArgs>(this.OnDrop);
            }
        }

        protected virtual void OnDrop(DragEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dependencyObject)
            {
                if (dependencyObject.FindAncestor<global::FoxTunes.MiniPlaylist>() != null)
                {
                    //Hack hack hack ... cannot find a way to "handle" this async event.
                    //The action would be duplicated between us and MiniPlaylist.
                    return;
                }
            }
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
                    var task = this.AddToPlaylist(paths);
                }
                if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    var libraryHierarchyNode = e.Data.GetData(typeof(LibraryHierarchyNode)) as LibraryHierarchyNode;
                    var task = this.AddToPlaylist(libraryHierarchyNode);
                }
                if (ShellIDListHelper.GetDataPresent(e.Data))
                {
                    var paths = ShellIDListHelper.GetData(e.Data);
                    var task = this.AddToPlaylist(paths);
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to process clipboard contents: {0}", exception.Message);
            }
        }

        private async Task AddToPlaylist(IEnumerable<string> paths)
        {
            var clear = default(bool);
            var play = default(bool);
            switch (MiniPlayerBehaviourConfiguration.GetDropBehaviour(this.DropCommit.Value))
            {
                case MiniPlayerDropBehaviour.Append:
                    clear = false;
                    play = false;
                    break;
                case MiniPlayerDropBehaviour.AppendAndPlay:
                    clear = false;
                    play = true;
                    break;
                case MiniPlayerDropBehaviour.Replace:
                    clear = true;
                    play = false;
                    break;
                case MiniPlayerDropBehaviour.ReplaceAndPlay:
                    clear = true;
                    play = true;
                    break;
            }
            var playlist = this.GetPlaylist();
            var index = this.PlaylistBrowser.GetInsertIndex(playlist);
            if (clear)
            {
                await this.PlaylistManager.Clear(playlist);
            }
            await this.FileActionHandlerManager.RunPaths(paths, index, FileActionType.Playlist).ConfigureAwait(false);
            if (play)
            {
                await this.PlaylistManager.Play(playlist, index).ConfigureAwait(false);
            }
        }

        private Task AddToPlaylist(LibraryHierarchyNode libraryHierarchyNode)
        {
            var playlist = this.GetPlaylist();
            if (playlist != null)
            {
                return this.PlaylistManager.Add(playlist, libraryHierarchyNode, false);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Playlist GetPlaylist()
        {
            if (this.PlaylistManager.CurrentPlaylist != null)
            {
                return this.PlaylistManager.CurrentPlaylist;
            }
            return this.PlaylistManager.SelectedPlaylist;
        }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaylistManager = core.Managers.Playlist;
            this.FileActionHandlerManager = core.Managers.FileActionHandler;
            this.Configuration = core.Components.Configuration;
            this.Topmost = this.Configuration.GetElement<BooleanConfigurationElement>(
              MiniPlayerBehaviourConfiguration.SECTION,
              MiniPlayerBehaviourConfiguration.TOPMOST_ELEMENT
            );
            this.ShowArtwork = this.Configuration.GetElement<BooleanConfigurationElement>(
              MiniPlayerBehaviourConfiguration.SECTION,
              MiniPlayerBehaviourConfiguration.SHOW_ARTWORK_ELEMENT
            );
            this.ShowPlaylist = this.Configuration.GetElement<BooleanConfigurationElement>(
              MiniPlayerBehaviourConfiguration.SECTION,
              MiniPlayerBehaviourConfiguration.SHOW_PLAYLIST_ELEMENT
            );
            this.DropCommit = this.Configuration.GetElement<SelectionConfigurationElement>(
              MiniPlayerBehaviourConfiguration.SECTION,
              MiniPlayerBehaviourConfiguration.DROP_COMMIT_ELEMENT
            );
            base.InitializeComponent(core);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Mini();
        }
    }
}
