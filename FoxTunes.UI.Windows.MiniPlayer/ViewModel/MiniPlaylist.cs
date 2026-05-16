using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class MiniPlaylist : PlaylistBase, IValueConverter
    {
        protected override string EMPTY
        {
            get
            {
                return Strings.MiniPlaylist_Empty;
            }
        }

        public Playlist CurrentPlaylist { get; private set; }

        public IScriptingContext ScriptingContext { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public PlaylistItem SelectedItem
        {
            get
            {
                if (this.PlaylistManager == null || this.PlaylistManager.SelectedItems == null)
                {
                    return null;
                }
                return this.PlaylistManager.SelectedItems.FirstOrDefault();
            }
            set
            {
                if (this.PlaylistManager == null)
                {
                    return;
                }
                if (value != null)
                {
                    this.PlaylistManager.SelectedItems = new[]
                    {
                        value
                    };
                }
                else
                {
                    this.PlaylistManager.SelectedItems = null;
                }
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

        private string _ShortScript { get; set; }

        public string ShortScript
        {
            get
            {
                return this._ShortScript;
            }
        }

        public Task SetShortScript(string value)
        {
            this._ShortScript = value;
            return this.OnShortScriptChanged();
        }

        protected virtual async Task OnShortScriptChanged()
        {
            await this.Refresh().ConfigureAwait(false);
            if (this.ShortScriptChanged != null)
            {
                this.ShortScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShortScript");
        }

        public event EventHandler ShortScriptChanged;

        private string _LongScript { get; set; }

        public string LongScript
        {
            get
            {
                return this._LongScript;
            }
        }

        public Task SetLongScript(string value)
        {
            this._LongScript = value;
            return this.OnLongScriptChanged();
        }

        protected virtual async Task OnLongScriptChanged()
        {
            await this.Refresh().ConfigureAwait(false);
            if (this.LongScriptChanged != null)
            {
                this.LongScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("LongScript");
        }

        public event EventHandler LongScriptChanged;

        private bool _ShowArtwork { get; set; }

        public bool ShowArtwork
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
            var task = this.RefreshItems();
            if (this.ShowArtworkChanged != null)
            {
                this.ShowArtworkChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowArtwork");
        }

        public event EventHandler ShowArtworkChanged;

        protected override Playlist GetPlaylist()
        {
            var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            return playlist;
        }

        protected override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.ScriptingContext = this.ScriptingRuntime.CreateContext();
            this.PlaylistManager.SelectedItemsChanged += this.OnSelectedItemsChanged;
            this.PlaylistManager.CurrentPlaylistChanged += this.OnCurrentPlaylistChanged;
            this.PlaylistManager.SelectedPlaylistChanged += this.OnSelectedPlaylistChanged;
            this.PlaylistManager.CurrentItemChanged += this.OnCurrentItemChanged;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                MiniPlayerBehaviourConfiguration.SECTION,
                MiniPlayerBehaviourConfiguration.PLAYLIST_SCRIPT_SHORT
            ).ConnectValue(async value => await this.SetShortScript(value).ConfigureAwait(false));
            this.Configuration.GetElement<TextConfigurationElement>(
                MiniPlayerBehaviourConfiguration.SECTION,
                MiniPlayerBehaviourConfiguration.PLAYLIST_SCRIPT_LONG
            ).ConnectValue(async value => await this.SetLongScript(value).ConfigureAwait(false));
            this.Configuration.GetElement<BooleanConfigurationElement>(
                MiniPlayerBehaviourConfiguration.SECTION,
                MiniPlayerBehaviourConfiguration.PLAYLIST_ARTWORK
            ).ConnectValue(value => { var task = Windows.Invoke(() => this.ShowArtwork = value); });
        }

        protected virtual void OnSelectedItemsChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnSelectedItemChanged);
        }

        protected virtual void OnCurrentPlaylistChanged(object sender, EventArgs e)
        {
            var task = this.RefreshIfRequired();
        }

        protected virtual void OnSelectedPlaylistChanged(object sender, EventArgs e)
        {
            var task = this.RefreshIfRequired();
        }

        protected virtual void OnCurrentItemChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.RefreshSelectedItem);
        }

        protected virtual Task RefreshIfRequired()
        {
            var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            if (object.ReferenceEquals(this.CurrentPlaylist, playlist))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Refresh();
        }

        public override async Task Refresh()
        {
            this.CurrentPlaylist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            await base.Refresh().ConfigureAwait(false);
            await this.RefreshSelectedItem().ConfigureAwait(false);
        }

        public virtual Task RefreshSelectedItem()
        {
            return Windows.Invoke(new Action(this.OnSelectedItemChanged));
        }

        public ICommand PlaySelectedItemCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                    () =>
                    {
                        return this.PlaylistManager.Play(this.SelectedItem);
                    },
                    () => this.PlaylistManager != null && this.SelectedItem != null
                );
            }
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
                else if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    effects = DragDropEffects.Copy;
                }
                else if (ShellIDListHelper.GetDataPresent(e.Data))
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
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
                    var task = this.AddToPlaylist(paths);
                }
                else if (e.Data.GetDataPresent(typeof(LibraryHierarchyNode)))
                {
                    var libraryHierarchyNode = e.Data.GetData(typeof(LibraryHierarchyNode)) as LibraryHierarchyNode;
                    var task = this.AddToPlaylist(libraryHierarchyNode);
                }
                else if (ShellIDListHelper.GetDataPresent(e.Data))
                {
                    var paths = ShellIDListHelper.GetData(e.Data);
                    var task = this.AddToPlaylist(paths);
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to process clipboard contents: {0}", exception.Message);
            }
            e.Handled = true;
        }

        private Task AddToPlaylist(IEnumerable<string> paths)
        {
            return this.FileActionHandlerManager.RunPaths(paths, FileActionType.Playlist);
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var playlistItem = value as PlaylistItem;
            if (playlistItem == null)
            {
                return null;
            }
            var script = default(string);
            if (this.ShowArtwork)
            {
                script = this.LongScript;
            }
            else
            {
                script = this.ShortScript;
            }
            var playlistItemScriptRunner = new PlaylistItemScriptRunner(this.ScriptingContext, playlistItem, script);
            playlistItemScriptRunner.Prepare();
            return playlistItemScriptRunner.Run();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override void OnDisposing()
        {
            if (this.PlaylistManager != null)
            {
                this.PlaylistManager.SelectedItemsChanged -= this.OnSelectedItemsChanged;
                this.PlaylistManager.CurrentPlaylistChanged -= this.OnCurrentPlaylistChanged;
                this.PlaylistManager.SelectedPlaylistChanged -= this.OnSelectedPlaylistChanged;
                this.PlaylistManager.CurrentItemChanged -= this.OnCurrentItemChanged;
            }
            if (this.ScriptingContext != null)
            {
                this.ScriptingContext.Dispose();
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MiniPlaylist();
        }
    }
}
