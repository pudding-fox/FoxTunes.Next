using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Playlists : ViewModelBase
    {
        public virtual bool EnabledOnly
        {
            get
            {
                return true;
            }
        }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        private PlaylistCollection _Items { get; set; }

        public PlaylistCollection Items
        {
            get
            {
                return this._Items;
            }
            set
            {
                this._Items = value;
                this.OnItemsChanged();
            }
        }

        protected virtual void OnItemsChanged()
        {
            if (this.ItemsChanged != null)
            {
                this.ItemsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Items");
        }

        public event EventHandler ItemsChanged;

        public Playlist SelectedItem
        {
            get
            {
                if (this.PlaylistManager == null)
                {
                    return Playlist.Empty;
                }
                return this.PlaylistManager.SelectedPlaylist;
            }
            set
            {
                if (this.PlaylistManager == null || value == null)
                {
                    return;
                }
                this.PlaylistManager.SelectedPlaylist = value;
            }
        }

        protected virtual void OnSelectedItemChanged()
        {
            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedItem");
        }

        public event EventHandler SelectedItemChanged;

        protected virtual Task RefreshItems()
        {
            var playlists = this.PlaylistBrowser.GetPlaylists();
            if (this.EnabledOnly)
            {
                playlists = playlists.Where(
                    playlist => playlist.Enabled
                ).ToArray();
            }
            if (this.Items == null)
            {
                return Windows.Invoke(() => this.Items = new PlaylistCollection(playlists));
            }
            else
            {
                return Windows.Invoke(this.Items.Reset(playlists));
            }
        }

        protected virtual Task RefreshItems(IEnumerable<Playlist> playlists, DataSignalType type)
        {
            var cached = this.PlaylistBrowser.GetPlaylists();
            if (this.EnabledOnly)
            {
                cached = cached.Where(
                    playlist => playlist.Enabled
                ).ToArray();
                playlists = playlists.Where(
                    playlist => playlist.Enabled
                ).ToArray();
            }
            if (this.Items == null)
            {
                return Windows.Invoke(() => this.Items = new PlaylistCollection(cached));
            }
            else
            {
                return Windows.Invoke(() =>
                {
                    switch (type)
                    {
                        case DataSignalType.None:
                            this.Items.Reset(cached)();
                            break;
                        case DataSignalType.Added:
                        case DataSignalType.Updated:
                            this.Items.AddOrUpdate(playlists);
                            break;
                        case DataSignalType.Removed:
                            this.Items.Remove(playlists);
                            break;
                    }
                });
            }
        }

        public virtual async Task Refresh(IEnumerable<Playlist> playlists, DataSignalType type)
        {
            await this.RefreshItems(playlists, type).ConfigureAwait(false);
            await Windows.Invoke(() =>
            {
                this.OnSelectedItemChanged();
            }).ConfigureAwait(false);
        }

        public virtual async Task Refresh()
        {
            await this.RefreshItems().ConfigureAwait(false);
            await Windows.Invoke(() =>
            {
                this.OnSelectedItemChanged();
            }).ConfigureAwait(false);
        }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    return this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null && state.Playlists.Any())
            {
                return this.Refresh(state.Playlists, state.Type);
            }
            else
            {
                return this.Refresh();
            }
        }

        protected virtual void OnSelectedPlaylistChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(new Action(this.OnSelectedItemChanged));
        }

        public Task AddPlaylist(LibraryHierarchyNode libraryHierarchyNode)
        {
            return PlaylistsActionsBehaviour.Instance.AddPlaylist(libraryHierarchyNode);
        }

        public Task AddPlaylist(IEnumerable<PlaylistItem> playlistItems)
        {
            return PlaylistsActionsBehaviour.Instance.AddPlaylist(playlistItems);
        }

        public Task AddPlaylist(IEnumerable<string> paths)
        {
            return PlaylistsActionsBehaviour.Instance.AddPlaylist(paths);
        }

        public ICommand RemovePlaylistCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.RemovePlaylist);
            }
        }

        public Task RemovePlaylist()
        {
            return PlaylistsActionsBehaviour.Instance.RemovePlaylist();
        }

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
                if (e.Data.GetDataPresent<IEnumerable<PlaylistItem>>())
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

        public ICommand AddPlaylistCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<RoutedEventArgs>(
                    new Func<RoutedEventArgs, Task>(this.AddPlaylist)
                );
            }
        }

        protected virtual Task AddPlaylist(RoutedEventArgs e)
        {
            if (e is DragEventArgs de)
            {
                return this.AddPlaylist(de);
            }
            else
            {
                return this.AddPlaylist();
            }
        }

        protected virtual Task AddPlaylist()
        {
            return PlaylistsActionsBehaviour.Instance.AddPlaylist();
        }

        protected virtual Task AddPlaylist(DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
                    return this.AddPlaylist(paths);
                }
                if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    var libraryHierarchyNode = e.Data.GetData(typeof(LibraryHierarchyNode)) as LibraryHierarchyNode;
                    return this.AddPlaylist(libraryHierarchyNode);
                }
                if (e.Data.GetDataPresent<IEnumerable<PlaylistItem>>())
                {
                    var playlistItems = e.Data
                        .GetData<IEnumerable<PlaylistItem>>()
                        .OrderBy(playlistItem => playlistItem.Sequence);
                    return this.AddPlaylist(playlistItems);
                }
                if (ShellIDListHelper.GetDataPresent(e.Data))
                {
                    var paths = ShellIDListHelper.GetData(e.Data);
                    return this.AddPlaylist(paths);
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to process clipboard contents: {0}", exception.Message);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public ICommand AddToPlaylistCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<DragEventArgs>(
                    new Func<DragEventArgs, Task>(this.AddToPlaylist)
                );
            }
        }

        protected virtual Task AddToPlaylist(DragEventArgs e)
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
                    return PlaylistActionsBehaviour.Instance.Add(playlist, paths, false);
                }
                if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    var libraryHierarchyNode = e.Data.GetData(typeof(LibraryHierarchyNode)) as LibraryHierarchyNode;
                    return PlaylistActionsBehaviour.Instance.Add(playlist, libraryHierarchyNode, false);
                }
                if (e.Data.GetDataPresent<IEnumerable<PlaylistItem>>())
                {
                    var playlistItems = e.Data
                        .GetData<IEnumerable<PlaylistItem>>()
                        .OrderBy(playlistItem => playlistItem.Sequence);
                    return PlaylistActionsBehaviour.Instance.Add(playlist, playlistItems, false);
                }
                if (ShellIDListHelper.GetDataPresent(e.Data))
                {
                    var paths = ShellIDListHelper.GetData(e.Data);
                    return PlaylistActionsBehaviour.Instance.Add(playlist, paths, false);
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to process clipboard contents: {0}", exception.Message);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public ICommand PlaylistSelectedCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.PlaylistSelected);
            }
        }

        public void PlaylistSelected()
        {
            this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistSelected, SignalState.None));
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Playlists();
        }

        protected override void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.SelectedPlaylistChanged -= this.OnSelectedPlaylistChanged;
            }
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }
    }
}
