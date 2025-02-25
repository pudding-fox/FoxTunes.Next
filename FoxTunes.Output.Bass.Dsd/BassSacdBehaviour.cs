using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassSacdBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent, IFileActionHandler
    {
        public const string ISO = ".iso";

        public const string OPEN_SACD = "FFFG";

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassSacdBehaviourConfiguration.SECTION,
                BassSacdBehaviourConfiguration.ENABLED
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
                if (this.Enabled.Value)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, OPEN_SACD, Strings.BassSacdBehaviour_Open);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case OPEN_SACD:
                    return this.OpenSacd();
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
            if (type != FileActionType.Playlist && type != FileActionType.Library)
            {
                return false;
            }
            if (!File.Exists(path) || !string.Equals(path.GetExtension(), "iso", StringComparison.OrdinalIgnoreCase))
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
                    var playlist = this.PlaylistManager.SelectedPlaylist;
                    return this.AddSacdToPlaylist(playlist, paths);
                case FileActionType.Library:
                    return this.AddSacdToLibrary(paths);
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
                    var playlist = this.PlaylistManager.SelectedPlaylist;
                    return this.AddSacdToPlaylist(playlist, index, paths);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task OpenSacd()
        {
            var options = new BrowseOptions(
                "Open",
                string.Empty,
                new[]
                {
                    new BrowseFilter("Isos", new[]
                    {
                        ISO
                    })
                },
                BrowseFlags.File
            );
            var result = this.FileSystemBrowser.Browse(options);
            if (!result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.AddSacdToPlaylist(this.PlaylistManager.SelectedPlaylist, result.Paths);
        }

        public Task AddSacdToLibrary(IEnumerable<string> paths)
        {
            throw new NotImplementedException();
        }

        public Task AddSacdToPlaylist(Playlist playlist, IEnumerable<string> paths)
        {
            var index = this.PlaylistBrowser.GetInsertIndex(playlist);
            return this.AddSacdToPlaylist(playlist, index, paths);
        }

        public async Task AddSacdToPlaylist(Playlist playlist, int index, IEnumerable<string> paths)
        {
            using (var task = new AddSacdToPlaylistTask(playlist, index, paths, false))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassSacdBehaviourConfiguration.GetConfigurationSections();
        }

        public class AddSacdToPlaylistTask : PlaylistTaskBase
        {
            public AddSacdToPlaylistTask(Playlist playlist, int sequence, IEnumerable<string> paths, bool clear) : base(playlist, sequence)
            {
                this.Paths = paths;
                this.Clear = clear;
            }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public override bool Cancellable
            {
                get
                {
                    return true;
                }
            }

            public IEnumerable<string> Paths { get; private set; }

            public bool Clear { get; private set; }

            public SacdPlaylistItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.Factory = new SacdPlaylistItemFactory(this);
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                if (this.Clear)
                {
                    await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
                }
                await this.AddPaths(this.Paths).ConfigureAwait(false);
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
                var playlistItems = default(PlaylistItem[]);
                await this.WithSubTask(this.Factory, async () => playlistItems = await this.Factory.Create(paths).ConfigureAwait(false)).ConfigureAwait(false);
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
    }
}
