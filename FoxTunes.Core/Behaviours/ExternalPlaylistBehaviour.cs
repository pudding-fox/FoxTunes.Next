using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class ExternalPlaylistBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent, IFileActionHandler
    {
        public const string LOAD_PLAYLIST = "YYYZ";

        public const string SAVE_PLAYLIST = "YYZZ";

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaylistManager = core.Managers.Playlist;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                ExternalPlaylistBehaviourConfiguration.SECTION,
                ExternalPlaylistBehaviourConfiguration.ENABLED
            );
            base.InitializeComponent(core);
        }

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
                if (this.Enabled.Value && this.PlaylistManager.SelectedPlaylist != null)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, LOAD_PLAYLIST, Strings.ExternalPlaylistBehaviour_Load, path: Strings.ExternalPlaylistBehaviour_Path);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SAVE_PLAYLIST, Strings.ExternalPlaylistBehaviour_Save, path: Strings.ExternalPlaylistBehaviour_Path);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case LOAD_PLAYLIST:
                    return this.Load();
                case SAVE_PLAYLIST:
                    return this.Save();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public bool CanHandle(string path, FileActionType type)
        {
            if (!this.Enabled.Value)
            {
                return false;
            }
            if (type != FileActionType.Playlist)
            {
                return false;
            }
            if (!File.Exists(path) || !PLSHelper.EXTENSIONS.Contains(path.GetExtension(), StringComparer.OrdinalIgnoreCase))
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
                    var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
                    var index = this.PlaylistBrowser.GetInsertIndex(playlist);
                    return this.Load(playlist, index, paths, false);
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
                    var playlist = this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
                    return this.Load(playlist, index, paths, false);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Load()
        {
            var options = new BrowseOptions(
                Strings.ExternalPlaylistBehaviour_Browse_Type,
                string.Empty,
                new[]
                {
                    new BrowseFilter(Strings.ExternalPlaylistBehaviour_Browse_Type, PLSHelper.EXTENSIONS)
                },
                BrowseFlags.File
            );
            var result = this.FileSystemBrowser.Browse(options);
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null || !result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var index = this.PlaylistBrowser.GetInsertIndex(playlist);
            return this.Load(playlist, index, result.Paths, false);
        }

        public async Task Load(Playlist playlist, int index, IEnumerable<string> fileNames, bool clear)
        {
            if (!fileNames.Any())
            {
                return;
            }
            using (var task = new LoadPlaylistsTask(playlist, index, fileNames, false))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task Save()
        {
            var options = new BrowseOptions(
                Strings.ExternalPlaylistBehaviour_Browse_Type,
                string.Empty,
                new[]
                {
                    new BrowseFilter(Strings.ExternalPlaylistBehaviour_Browse_Type, PLSHelper.EXTENSIONS)
                },
                BrowseFlags.File | BrowseFlags.Save
            );
            var result = this.FileSystemBrowser.Browse(options);
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null || !result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Save(playlist, result.Paths.FirstOrDefault());
        }

        public async Task Save(Playlist playlist, string path)
        {
            using (var task = new SavePlaylistTask(playlist, path))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ExternalPlaylistBehaviourConfiguration.GetConfigurationSections();
        }

        public class LoadPlaylistsTask : PlaylistTaskBase
        {
            public LoadPlaylistsTask(Playlist playlist, int sequence, IEnumerable<string> fileNames, bool clear) : base(playlist, sequence)
            {
                this.FileNames = fileNames;
                this.Clear = clear;
            }

            public IEnumerable<string> FileNames { get; private set; }

            public bool Clear { get; private set; }

            public PLSHelper.PlaylistItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.Factory = new PLSHelper.PlaylistItemFactory();
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                if (this.Clear)
                {
                    await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
                }
                await this.AddPaths(this.FileNames).ConfigureAwait(false);
            }

            protected override async Task AddPaths(IEnumerable<string> paths)
            {
                using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    await this.AddPlaylistItems(paths, cancellationToken).ConfigureAwait(false);
                    await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
                    await this.SequenceItems().ConfigureAwait(false);
                    await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
            }

            protected override async Task AddPlaylistItems(IEnumerable<string> paths, CancellationToken cancellationToken)
            {
                var playlistItems = await this.Factory.Create(paths).ConfigureAwait(false);
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<PlaylistItem>(transaction);
                    foreach (var playlistItem in playlistItems)
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", playlistItem.FileName);
                        playlistItem.Playlist_Id = this.Playlist.Id;
                        playlistItem.Sequence = this.Sequence;
                        playlistItem.Status = PlaylistItemStatus.Import;
                        await set.AddAsync(playlistItem).ConfigureAwait(false);
                        this.Offset++;
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }

            protected override Task OnCompleted()
            {
                return this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated)));
            }
        }

        public class SavePlaylistTask : PlaylistTaskBase
        {
            public SavePlaylistTask(Playlist playlist, string fileName) : base(playlist)
            {
                this.FileName = fileName;
            }

            public string FileName { get; private set; }

            public IPlaylistBrowser PlaylistBrowser { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.PlaylistBrowser = core.Components.PlaylistBrowser;
                base.InitializeComponent(core);
            }

            protected override Task OnRun()
            {
                var playlistItems = this.PlaylistBrowser.GetItems(this.Playlist);
                if (playlistItems.Any())
                {
                    using (var writer = PLSHelper.Writer.ToFile(playlistItems, this.FileName))
                    {
                        writer.Write();
                    }
                }
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }
    }
}
