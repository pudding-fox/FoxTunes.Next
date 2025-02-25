using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class PlaylistManager : StandardManager, IPlaylistManager
    {
        public const string CLEAR_PLAYLIST = "ZZZZ";

        public PlaylistManager()
        {
            this._SelectedItems = new ConcurrentDictionary<Playlist, PlaylistItem[]>();
        }

        private volatile bool IsNavigating = false;

        public ICore Core { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.DatabaseFactory = core.Factories.Database;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null && state.Playlists.Any())
            {
                this.RefreshSelectedItems(state.Playlists);
                if (this.SelectedPlaylist == null || state.Playlists.Contains(this.SelectedPlaylist))
                {
                    this.RefreshSelectedPlaylist();
                }
                if (this.CurrentPlaylist != null && state.Playlists.Contains(this.CurrentPlaylist))
                {
                    this.RefreshCurrentPlaylist();
                }
                if (this.CurrentItem != null && state.Playlists.Any(playlist => playlist.Id == this.CurrentItem.Playlist_Id))
                {
                    this.RefreshCurrentItem();
                }
            }
            else
            {
                this.Refresh();
            }
        }

        public void Refresh()
        {
            this.RefreshSelectedItems();
            this.RefreshSelectedPlaylist();
            this.RefreshCurrentPlaylist();
            this.RefreshCurrentItem();
        }

        protected virtual void RefreshSelectedItems()
        {
            this.RefreshSelectedItems(this._SelectedItems.Keys);
        }

        protected virtual void RefreshSelectedItems(IEnumerable<Playlist> playlists)
        {
            foreach (var playlist in playlists)
            {
                this.RefreshSelectedItems(playlist);
            }
            this.OnSelectedItemsChanged();
        }

        protected virtual void RefreshSelectedItems(Playlist playlist)
        {
            var playlistItems = default(PlaylistItem[]);
            if (!this._SelectedItems.TryGetValue(playlist, out playlistItems))
            {
                return;
            }
            if (playlistItems == null)
            {
                return;
            }
            for (var a = 0; a < playlistItems.Length; a++)
            {
                if (playlistItems[a] == null)
                {
                    continue;
                }
                var playlistItem = this.PlaylistBrowser.GetItemById(playlist, playlistItems[a].Id);
                if (playlistItem == null)
                {
                    //TODO: Technically we should remove the item but it's an array so that's a pain.
                    continue;
                }
                playlistItems[a] = playlistItem;
            }
        }

        protected virtual void RefreshSelectedPlaylist()
        {
            var selectedPlaylist = this.SelectedPlaylist;
            if (selectedPlaylist != null)
            {
                selectedPlaylist = this.PlaylistBrowser.GetPlaylists().FirstOrDefault(playlist => playlist.Id == this.SelectedPlaylist.Id);
                if (selectedPlaylist != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshed selected playlist: {0} => {1}", this.SelectedPlaylist.Id, this.SelectedPlaylist.Name);
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to refresh selected playlist, it was removed or disabled.");
                }
            }
            if (selectedPlaylist == null)
            {
                selectedPlaylist = this.PlaylistBrowser.GetPlaylists().FirstOrDefault();
                if (selectedPlaylist == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to select a playlist, perhaps none are enabled?");
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Selected first playlist: {0} => {1}", selectedPlaylist.Id, selectedPlaylist.Name);
                }
            }
            if (object.ReferenceEquals(this.SelectedPlaylist, selectedPlaylist))
            {
                return;
            }
            this.SelectedPlaylist = selectedPlaylist;
        }

        protected virtual void RefreshCurrentPlaylist()
        {
            var currentPlaylist = this.CurrentPlaylist;
            if (currentPlaylist != null)
            {
                currentPlaylist = this.PlaylistBrowser.GetPlaylists().FirstOrDefault(playlist => playlist.Id == currentPlaylist.Id);
                if (currentPlaylist != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshed current playlist: {0} => {1}", currentPlaylist.Id, currentPlaylist.Name);
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to refresh current playlist, it was removed or disabled.");
                }
            }
            if (object.ReferenceEquals(this.CurrentPlaylist, currentPlaylist))
            {
                return;
            }
            this.CurrentPlaylist = currentPlaylist;
        }

        protected virtual void RefreshCurrentItem()
        {
            var currentItem = this.CurrentItem;
            if (currentItem != null)
            {
                var playlist = this.PlaylistBrowser.GetPlaylist(currentItem);
                if (playlist != null)
                {
                    currentItem = this.PlaylistBrowser.GetItems(playlist).FirstOrDefault(
                        playlistItem => playlistItem.Id == currentItem.Id && string.Equals(currentItem.FileName, playlistItem.FileName, StringComparison.OrdinalIgnoreCase)
                    );
                }
                if (currentItem == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to refresh current item.");
                }
            }
            if (object.ReferenceEquals(this.CurrentItem, currentItem))
            {
                return;
            }
            this.CurrentItem = currentItem;
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Playback manager output stream changed, updating current playlist item.");
            if (this.PlaybackManager.CurrentStream == null)
            {
                this.CurrentItem = null;
                Logger.Write(this, LogLevel.Debug, "Playback manager output stream is empty. Cleared current playlist item.");
            }
            else if (this.PlaybackManager.CurrentStream.PlaylistItem != this.CurrentItem)
            {
                this.CurrentItem = this.PlaybackManager.CurrentStream.PlaylistItem;
                Logger.Write(this, LogLevel.Debug, "Updated current playlist item: {0} => {1}", this.CurrentItem.Id, this.CurrentItem.FileName);
            }
        }

        public async Task Create(Playlist playlist)
        {
            using (var task = new AddPlaylistTask(playlist))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Create(Playlist playlist, IEnumerable<string> paths)
        {
            using (var task = new AddPlaylistTask(playlist, paths))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Create(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode)
        {
            using (var task = new AddPlaylistTask(playlist, libraryHierarchyNode))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Create(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
        {
            using (var task = new AddPlaylistTask(playlist, playlistItems))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Remove(Playlist playlist)
        {
            using (var task = new RemovePlaylistTask(playlist))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Add(Playlist playlist, IEnumerable<string> paths, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding paths to playlist.");
            var index = this.PlaylistBrowser.GetInsertIndex(this.SelectedPlaylist);
            return this.Insert(playlist, index, paths, clear);
        }

        public async Task Insert(Playlist playlist, int index, IEnumerable<string> paths, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting paths into playlist at index: {0}", index);
            using (var task = new AddPathsToPlaylistTask(playlist, index, paths, clear))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Add(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding library node to playlist.");
            var index = this.PlaylistBrowser.GetInsertIndex(this.SelectedPlaylist);
            return this.Insert(playlist, index, libraryHierarchyNode, clear);
        }

        public async Task Insert(Playlist playlist, int index, LibraryHierarchyNode libraryHierarchyNode, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting library node into playlist at index: {0}", index);
            using (var task = new AddLibraryHierarchyNodeToPlaylistTask(playlist, index, libraryHierarchyNode, clear))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Add(Playlist playlist, IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding library nodes to playlist.");
            var index = this.PlaylistBrowser.GetInsertIndex(this.SelectedPlaylist);
            return this.Insert(playlist, index, libraryHierarchyNodes, clear);
        }

        public async Task Insert(Playlist playlist, int index, IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting library nodes into playlist at index: {0}", index);
            using (var task = new AddLibraryHierarchyNodesToPlaylistTask(playlist, index, libraryHierarchyNodes, this.LibraryHierarchyBrowser.Filter, clear))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Add(Playlist playlist, IEnumerable<PlaylistItem> playlistItems, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Adding items to playlist.");
            var index = this.PlaylistBrowser.GetInsertIndex(this.SelectedPlaylist);
            return this.Insert(playlist, index, playlistItems, clear);
        }

        public async Task Insert(Playlist playlist, int index, IEnumerable<PlaylistItem> playlistItems, bool clear)
        {
            Logger.Write(this, LogLevel.Debug, "Inserting items into playlist at index: {0}", index);
            using (var task = new MovePlaylistItemsTask(playlist, index, playlistItems, clear))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Move(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
        {
            Logger.Write(this, LogLevel.Debug, "Re-ordering playlist items.");
            var index = this.PlaylistBrowser.GetInsertIndex(this.SelectedPlaylist);
            return this.Move(playlist, index, playlistItems);
        }

        public async Task Move(Playlist playlist, int index, IEnumerable<PlaylistItem> playlistItems)
        {
            Logger.Write(this, LogLevel.Debug, "Re-ordering playlist items at index: {0}", index);
            using (var task = new MovePlaylistItemsTask(playlist, index, playlistItems, false))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Remove(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
        {
            using (var task = new RemoveItemsFromPlaylistTask(playlist, playlistItems))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Crop(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var queryable = database.AsQueryable<PlaylistItem>(transaction);
                    var query = queryable.Where(
                        playlistItem => playlistItem.Playlist_Id == playlist.Id
                    ).Except(playlistItems);
                    //TODO: Warning: Buffering a potentially large sequence.
                    playlistItems = query.ToArray();
                }
            }
            using (var task = new RemoveItemsFromPlaylistTask(playlist, playlistItems))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Next()
        {
            return this.Next(true);
        }

        public async Task Next(bool wrap)
        {
            if (this.SelectedPlaylist == null)
            {
                Logger.Write(this, LogLevel.Debug, "No playlist.");
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Navigating to next playlist item.");
            if (this.IsNavigating)
            {
                Logger.Write(this, LogLevel.Debug, "Already navigating, ignoring request.");
                return;
            }
            try
            {
                this.IsNavigating = true;
                var playlistItem = this.PlaylistBrowser.GetNextItem(this.CurrentPlaylist, wrap);
                if (playlistItem == null)
                {
                    Logger.Write(this, LogLevel.Debug, "Playlist ended, stopping.");
                    await this.PlaybackManager.Stop();
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Playing playlist item: {0} => {1}", playlistItem.Id, playlistItem.FileName);
                    await this.Play(playlistItem).ConfigureAwait(false);
                }
            }
            finally
            {
                this.IsNavigating = false;
            }
        }

        public async Task Previous()
        {
            if (this.SelectedPlaylist == null)
            {
                Logger.Write(this, LogLevel.Debug, "No playlist.");
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Navigating to previous playlist item.");
            if (this.IsNavigating)
            {
                Logger.Write(this, LogLevel.Debug, "Already navigating, ignoring request.");
                return;
            }
            try
            {
                this.IsNavigating = true;
                var playlistItem = this.PlaylistBrowser.GetPreviousItem(this.CurrentPlaylist, true);
                if (playlistItem == null)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Playing playlist item: {0} => {1}", playlistItem.Id, playlistItem.FileName);
                await this.Play(playlistItem).ConfigureAwait(false);
            }
            finally
            {
                this.IsNavigating = false;
            }
        }

        public async Task Play(PlaylistItem playlistItem)
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream != null && outputStream.PlaylistItem == playlistItem && outputStream.IsReady)
            {
                await outputStream.Seek(0).ConfigureAwait(false);
                if (!outputStream.IsPlaying)
                {
                    await outputStream.Play().ConfigureAwait(false);
                }
                return;
            }
            var exception = default(Exception);
            try
            {
                await this.PlaybackManager.Load(playlistItem, true).ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.ErrorEmitter.Send(this, exception).ConfigureAwait(false);
        }

        public Task Play(Playlist playlist, int sequence)
        {
            var playlistItem = this.PlaylistBrowser.GetItemBySequence(playlist, sequence);
            if (playlistItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Play(playlistItem);
        }

        public async Task Clear(Playlist playlist)
        {
            var behaviour = default(PlaylistBehaviourBase);
            switch (playlist.Type)
            {
                case PlaylistType.Selection:
                    behaviour = ComponentRegistry.Instance.GetComponent<SelectionPlaylistBehaviour>();
                    break;
                case PlaylistType.Dynamic:
                    behaviour = ComponentRegistry.Instance.GetComponent<DynamicPlaylistBehaviour>();
                    break;
                case PlaylistType.Smart:
                    behaviour = ComponentRegistry.Instance.GetComponent<SmartPlaylistBehaviour>();
                    break;
                case PlaylistType.Everything:
                    behaviour = ComponentRegistry.Instance.GetComponent<EverythingPlaylistBehaviour>();
                    break;
            }
            if (behaviour != null)
            {
                await behaviour.Refresh(playlist, true).ConfigureAwait(false);
            }
            else
            {
                using (var task = new ClearPlaylistTask(playlist))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
        }

        public async Task<int> Sort(Playlist playlist, PlaylistColumn playlistColumn, bool descending)
        {
            using (var task = new SortPlaylistTask(playlist, playlistColumn, descending))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                return task.Changes;
            }
        }

        private Playlist _SelectedPlaylist { get; set; }

        public Playlist SelectedPlaylist
        {
            get
            {
                return this._SelectedPlaylist;
            }
            set
            {
                if (object.ReferenceEquals(this._SelectedPlaylist, value))
                {
                    return;
                }
                this._SelectedPlaylist = value;
                this.OnSelectedPlaylistChanged();
            }
        }

        protected virtual void OnSelectedPlaylistChanged()
        {
            if (this.SelectedPlaylistChanged != null)
            {
                this.SelectedPlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedPlaylist");
        }

        public event EventHandler SelectedPlaylistChanged;

        private Playlist _CurrentPlaylist { get; set; }

        public Playlist CurrentPlaylist
        {
            get
            {
                return this._CurrentPlaylist;
            }
            protected set
            {
                if (object.ReferenceEquals(this._CurrentPlaylist, value))
                {
                    return;
                }
                this._CurrentPlaylist = value;
                this.OnCurrentPlaylistChanged();
            }
        }

        protected virtual void OnCurrentPlaylistChanged()
        {
            if (this.CurrentPlaylistChanged != null)
            {
                this.CurrentPlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentPlaylist");
        }

        public event EventHandler CurrentPlaylistChanged;

        private PlaylistItem _CurrentItem { get; set; }

        public PlaylistItem CurrentItem
        {
            get
            {
                return this._CurrentItem;
            }
            protected set
            {
                if (object.ReferenceEquals(this._CurrentItem, value))
                {
                    return;
                }
                this._CurrentItem = value;
                this.OnCurrentItemChanged();
            }
        }

        protected virtual void OnCurrentItemChanged()
        {
            if (this.CurrentItem == null)
            {
                this.CurrentPlaylist = null;
            }
            else
            {
                this.CurrentPlaylist = this.PlaylistBrowser.GetPlaylist(this.CurrentItem);
            }
            if (this.CurrentItemChanged != null)
            {
                this.CurrentItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentItem");
        }

        public event EventHandler CurrentItemChanged;

        private ConcurrentDictionary<Playlist, PlaylistItem[]> _SelectedItems { get; set; }

        public PlaylistItem[] SelectedItems
        {
            get
            {
                var playlistItems = default(PlaylistItem[]);
                if (this.SelectedPlaylist == null || !this._SelectedItems.TryGetValue(this.SelectedPlaylist, out playlistItems))
                {
                    return new PlaylistItem[] { };
                }
                return playlistItems;
            }
            set
            {
                if (this.SelectedPlaylist == null || object.ReferenceEquals(this.SelectedItems, value))
                {
                    return;
                }
                this._SelectedItems[this.SelectedPlaylist] = value;
                this.OnSelectedItemsChanged();
            }
        }

        protected virtual void OnSelectedItemsChanged()
        {
            if (this.SelectedItemsChanged != null)
            {
                this.SelectedItemsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedItems");
        }

        public event EventHandler SelectedItemsChanged;

        private string _Filter { get; set; }

        public string Filter
        {
            get
            {
                return this._Filter;
            }
            set
            {
                if (string.Equals(this._Filter, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                this._Filter = value;
                this.OnFilterChanged();
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

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.SelectedPlaylist != null)
                {
                    if (this.SelectedPlaylist.Type == PlaylistType.None)
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, CLEAR_PLAYLIST, Strings.PlaylistManager_Clear, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                    }
                    else
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, CLEAR_PLAYLIST, Strings.PlaylistManager_Reset, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case CLEAR_PLAYLIST:
                    return this.Clear(this.SelectedPlaylist);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public bool CanHandle(string path, FileActionType type)
        {
            if (type != FileActionType.Playlist)
            {
                return false;
            }
            if (!Directory.Exists(path) && !this.PlaybackManager.IsSupported(path))
            {
                return false;
            }
            return true;
        }

        public Task Handle(IEnumerable<string> paths, FileActionType type)
        {
            switch (type)
            {
                case FileActionType.Playlist:
                    return this.Add(this.SelectedPlaylist, paths, false);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Handle(IEnumerable<string> paths, int index, FileActionType type)
        {
            switch (type)
            {
                case FileActionType.Playlist:
                    return this.Insert(this.SelectedPlaylist, index, paths, false);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public string Checksum
        {
            get
            {
                return "81B4B54D-8671-4B69-A73D-5068244A2181";
            }
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            //IMPORTANT: When editing this function remember to change the checksum.
            if (!type.HasFlag(DatabaseInitializeType.Playlist))
            {
                return;
            }
            var scriptingRuntime = ComponentRegistry.Instance.GetComponent<IScriptingRuntime>();
            if (scriptingRuntime == null)
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                {
                    var set = database.Set<Playlist>(transaction);
                    foreach (var playlist in set)
                    {
                        //TODO: Bad .Wait()
                        PlaylistTaskBase.RemovePlaylistItems(database, playlist.Id, PlaylistItemStatus.None, transaction).Wait();
                    }
                    set.Clear();
                    set.Add(new Playlist() { Name = "Default", Type = PlaylistType.None, Enabled = true });
                }
                {
                    var set = database.Set<PlaylistColumn>(transaction);
                    set.Clear();
                    set.Add(new PlaylistColumn() { Name = "Artist / album", Type = PlaylistColumnType.Script, Sequence = 1, Script = scriptingRuntime.CoreScripts.Artist_Album, Width = PlaylistColumn.WIDTH_LARGE, Enabled = true });
                    set.Add(new PlaylistColumn() { Name = "Track no", Type = PlaylistColumnType.Script, Sequence = 2, Script = scriptingRuntime.CoreScripts.Track, Enabled = true });
                    set.Add(new PlaylistColumn() { Name = "Title / track artist", Type = PlaylistColumnType.Script, Sequence = 3, Script = scriptingRuntime.CoreScripts.Title_Performer, Width = PlaylistColumn.WIDTH_LARGE, Enabled = true });
                    set.Add(new PlaylistColumn() { Name = "Duration", Type = PlaylistColumnType.Tag, Sequence = 4, Tag = CommonProperties.Duration, Format = CommonFormats.TimeSpan, Enabled = true });
                    set.Add(new PlaylistColumn() { Name = "Codec", Type = PlaylistColumnType.Script, Sequence = 5, Script = scriptingRuntime.CoreScripts.Codec, Enabled = true });
                    set.Add(new PlaylistColumn() { Name = "Genre", Type = PlaylistColumnType.Script, Sequence = 6, Script = scriptingRuntime.CoreScripts.Genre, Width = PlaylistColumn.WIDTH_MEDIUM, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "BPM", Type = PlaylistColumnType.Tag, Sequence = 7, Tag = CommonMetaData.BeatsPerMinute, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Album gain", Type = PlaylistColumnType.Tag, Sequence = 8, Tag = CommonMetaData.ReplayGainAlbumGain, Format = CommonFormats.Decibel, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Album peak", Type = PlaylistColumnType.Tag, Sequence = 9, Tag = CommonMetaData.ReplayGainAlbumPeak, Format = CommonFormats.Float, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Track gain", Type = PlaylistColumnType.Tag, Sequence = 10, Tag = CommonMetaData.ReplayGainTrackGain, Format = CommonFormats.Decibel, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Track peak", Type = PlaylistColumnType.Tag, Sequence = 11, Tag = CommonMetaData.ReplayGainTrackPeak, Format = CommonFormats.Float, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Play count", Type = PlaylistColumnType.Tag, Sequence = 12, Tag = CommonStatistics.PlayCount, Format = CommonFormats.Integer, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Last played", Type = PlaylistColumnType.Tag, Sequence = 13, Tag = CommonStatistics.LastPlayed, Format = CommonFormats.TimeStamp, Enabled = false });
                    set.Add(new PlaylistColumn() { Name = "Initial key", Type = PlaylistColumnType.Tag, Sequence = 14, Tag = CommonMetaData.InitialKey, Enabled = false });
                }
                transaction.Commit();
            }
        }
    }
}
