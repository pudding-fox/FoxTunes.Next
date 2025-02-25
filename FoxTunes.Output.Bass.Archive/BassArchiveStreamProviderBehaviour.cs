#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using ManagedBass.ZipStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    //TODO: bass_zipstream.dll does not work on XP.
    //TODO: Unable to avoid linking to CxxFrameHandler3 which does not exist until a later version of msvcrt.dll
    [PlatformDependency(Major = 6, Minor = 0)]
    public class BassArchiveStreamProviderBehaviour : StandardBehaviour, IConfigurableComponent, IInvocableComponent, IFileActionHandler
    {
        public const string OPEN_ARCHIVE = "FGGG";

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassArchiveStreamProvider).Assembly.Location);
            }
        }

        public BassArchiveStreamProviderBehaviour()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_zipstream.dll"));
        }

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public int BufferMin
        {
            get
            {
                var value = default(int);
                if (!BassZipStream.GetConfig(BassZipStreamAttribute.BufferMin, out value))
                {
                    value = BassZipStream.DEFAULT_BUFFER_MIN;
                }
                return value;
            }
            set
            {
                BassZipStream.SetConfig(BassZipStreamAttribute.BufferMin, value);
                Logger.Write(this, LogLevel.Debug, "BufferMin = {0}", this.BufferMin);
            }
        }

        public int BufferTimeout
        {
            get
            {
                var value = default(int);
                if (!BassZipStream.GetConfig(BassZipStreamAttribute.BufferTimeout, out value))
                {
                    value = BassZipStream.DEFAULT_BUFFER_TIMEOUT;
                }
                return value;
            }
            set
            {
                BassZipStream.SetConfig(BassZipStreamAttribute.BufferTimeout, value);
                Logger.Write(this, LogLevel.Debug, "BufferTimeout = {0}", this.BufferTimeout);
            }
        }

        public bool DoubleBuffer
        {
            get
            {
                var value = default(bool);
                if (!BassZipStream.GetConfig(BassZipStreamAttribute.DoubleBuffer, out value))
                {
                    value = BassZipStream.DEFAULT_DOUBLE_BUFFER;
                }
                return value;
            }
            set
            {
                BassZipStream.SetConfig(BassZipStreamAttribute.DoubleBuffer, value);
                Logger.Write(this, LogLevel.Debug, "DoubleBuffer = {0}", this.DoubleBuffer);
            }
        }

        private bool _Enabled { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                Logger.Write(this, LogLevel.Debug, "Enabled = {0}", this.Enabled);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.BUFFER_MIN_ELEMENT
            ).ConnectValue(value => this.BufferMin = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.BUFFER_TIMEOUT_ELEMENT
            ).ConnectValue(value => this.BufferTimeout = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.DOUBLE_BUFFER_ELEMENT
            ).ConnectValue(value => this.DoubleBuffer = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => this.Enabled = value);
            base.InitializeComponent(core);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassArchiveStreamProviderBehaviourConfiguration.GetConfigurationSections();
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
                if (this.Enabled)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, OPEN_ARCHIVE, Strings.BassArchiveStreamProviderBehaviour_Open);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case OPEN_ARCHIVE:
                    return this.OpenArchive();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public bool CanHandle(string path, FileActionType type)
        {
            if (!this.Enabled)
            {
                return false;
            }
            if (type != FileActionType.Playlist && type != FileActionType.Library)
            {
                return false;
            }
            if (!File.Exists(path) || !ArchiveUtils.Extensions.Contains(path.GetExtension(), StringComparer.OrdinalIgnoreCase))
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
                    return this.AddArchivesToPlaylist(playlist, paths);
                case FileActionType.Library:
                    return this.AddArchivesToLibrary(paths);
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
                    return this.AddArchivesToPlaylist(playlist, index, paths);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task OpenArchive()
        {
            var options = new BrowseOptions(
                "Open",
                string.Empty,
                new[]
                {
                    new BrowseFilter("Archives", ArchiveUtils.Extensions)
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
            return this.AddArchivesToPlaylist(this.PlaylistManager.SelectedPlaylist, result.Paths);
        }

        public async Task AddArchivesToLibrary(IEnumerable<string> paths)
        {
            using (var task = new AddArchivesToLibraryTask(paths))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task AddArchivesToPlaylist(Playlist playlist, IEnumerable<string> paths)
        {
            var index = this.PlaylistBrowser.GetInsertIndex(playlist);
            return this.AddArchivesToPlaylist(playlist, index, paths);
        }

        public async Task AddArchivesToPlaylist(Playlist playlist, int index, IEnumerable<string> paths)
        {
            using (var task = new AddArchivesToPlaylistTask(playlist, index, paths, false))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        private class AddArchivesToPlaylistTask : PlaylistTaskBase
        {
            public AddArchivesToPlaylistTask(Playlist playlist, int sequence, IEnumerable<string> paths, bool clear) : base(playlist, sequence)
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

            public IEnumerable<string> Paths { get; private set; }

            public bool Clear { get; private set; }

            public IPlaylistBrowser PlaylistBrowser { get; private set; }

            public ArchivePlaylistItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                //Clear any cached passwords if possible.
                var password = ComponentRegistry.Instance.GetComponent<BassArchiveStreamPasswordBehaviour>();
                if (password != null)
                {
                    password.Reset();
                }
                this.PlaylistBrowser = core.Components.PlaylistBrowser;
                this.Factory = new ArchivePlaylistItemFactory(this.Visible);
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override Task OnStarted()
            {
                this.Name = "Opening archive";
                return base.OnStarted();
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

        public class AddArchivesToLibraryTask : LibraryTaskBase
        {
            public AddArchivesToLibraryTask(IEnumerable<string> paths)
            {
                this.Paths = paths;
            }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public IEnumerable<string> Roots
            {
                get
                {
                    return this.Paths;
                }
            }

            public IEnumerable<string> Paths { get; private set; }

            public ArchiveLibraryItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                //Clear any cached passwords if possible.
                var password = ComponentRegistry.Instance.GetComponent<BassArchiveStreamPasswordBehaviour>();
                if (password != null)
                {
                    password.Reset();
                }
                this.Factory = new ArchiveLibraryItemFactory(this.Visible);
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override Task OnStarted()
            {
                this.Name = "Opening archive";
                return base.OnStarted();
            }

            protected override async Task OnRun()
            {
                await this.AddRoots(this.Roots).ConfigureAwait(false);
                await this.AddPaths(this.Paths).ConfigureAwait(false);
            }

            protected override async Task AddPaths(IEnumerable<string> paths)
            {
                var libraryItems = default(LibraryItem[]);
                await this.WithSubTask(this.Factory, async () => libraryItems = await this.Factory.Create(paths).ConfigureAwait(false)).ConfigureAwait(false);
                using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    await this.RemoveLibraryItems().ConfigureAwait(false);
                    await this.AddLibraryItems(libraryItems).ConfigureAwait(false);
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
                await this.BuildHierarchies(LibraryItemStatus.Import).ConfigureAwait(false);
                await RemoveCancelledLibraryItems(this.Database).ConfigureAwait(false);
                await SetLibraryItemsStatus(this.Database, LibraryItemStatus.None).ConfigureAwait(false);
            }

            private async Task RemoveLibraryItems()
            {
                var cleanup = default(bool);
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    foreach (var root in this.Roots)
                    {
                        //Using source without relations.
                        //TODO: Add this kind of thing to FoxDb, we're actually performing some schema queries to create this structure.
                        var source = this.Database.Source(
                            this.Database.Config.Transient.Table<LibraryItem>(TableFlags.AutoColumns),
                            transaction
                        );
                        var set = this.Database.Set<LibraryItem>(source);
                        var libraryItems = set.Where(libraryItem => libraryItem.DirectoryName == root);
                        foreach (var libraryItem in libraryItems)
                        {
                            Logger.Write(this, LogLevel.Debug, "Removing file from library: {0}", libraryItem.FileName);
                            libraryItem.Status = LibraryItemStatus.Remove;
                            await set.AddOrUpdateAsync(libraryItem).ConfigureAwait(false);
                            cleanup = true;
                        }
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
                if (cleanup)
                {
                    await this.RemoveItems(LibraryItemStatus.Remove).ConfigureAwait(false);
                }
            }

            private async Task AddLibraryItems(LibraryItem[] libraryItems)
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<LibraryItem>(transaction);
                    foreach (var libraryItem in libraryItems)
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to library: {0}", libraryItem.FileName);
                        libraryItem.Status = LibraryItemStatus.Import;
                        libraryItem.SetImportDate(DateTime.UtcNow);
                        await set.AddAsync(libraryItem).ConfigureAwait(false);
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }

            protected override async Task OnCompleted()
            {
                await base.OnCompleted().ConfigureAwait(false);
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.LibraryUpdated)).ConfigureAwait(false);
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.HierarchiesUpdated)).ConfigureAwait(false);
            }
        }
    }
}