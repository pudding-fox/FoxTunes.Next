#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class LibraryTaskBase : BackgroundTask
    {
        public const string ID = "B6AF297E-F334-481D-8D60-BD5BE5935BD9";

        protected LibraryTaskBase()
            : base(ID)
        {
            this.Warnings = new Dictionary<LibraryItem, IList<string>>();
        }

        public IDictionary<LibraryItem, IList<string>> Warnings { get; private set; }

        public ICore Core { get; private set; }

        public IDatabaseComponent Database { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Database = core.Factories.Database.Create();
            this.PlaybackManager = core.Managers.Playback;
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected virtual async Task<IEnumerable<string>> GetRoots()
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var sequence = this.Database.Set<LibraryRoot>(transaction).GetAsyncEnumerator())
                {
                    var result = new List<string>();
                    while (await sequence.MoveNextAsync().ConfigureAwait(false))
                    {
                        result.Add(sequence.Current.DirectoryName);
                    }
                    return result;
                }
            }
        }

        protected virtual Task AddRoot(string path)
        {
            return this.AddRoots(new[] { path });
        }

        protected virtual async Task AddRoots(IEnumerable<string> paths)
        {
            var roots = await this.NormalizeRoots(paths).ConfigureAwait(false);
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                var set = this.Database.Set<LibraryRoot>(transaction);
                Logger.Write(this, LogLevel.Debug, "Clearing library roots.");
                await set.ClearAsync().ConfigureAwait(false);
                foreach (var path in roots)
                {
                    Logger.Write(this, LogLevel.Debug, "Creating library root: {0}", path);
                    await set.AddAsync(
                        set.Create().With(
                            libraryRoot => libraryRoot.DirectoryName = path
                        )
                    ).ConfigureAwait(false);
                }
                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        protected virtual async Task<IEnumerable<string>> NormalizeRoots(IEnumerable<string> newPaths)
        {
            var currentPaths = await this.GetRoots().ConfigureAwait(false);
            return LibraryRoot.Normalize(currentPaths, newPaths);
        }

        protected virtual async Task ClearRoots()
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                var set = this.Database.Set<LibraryRoot>(transaction);
                Logger.Write(this, LogLevel.Debug, "Clearing library roots.");
                await set.ClearAsync().ConfigureAwait(false);
                transaction.Commit();
            }
        }

        protected virtual async Task AddPaths(IEnumerable<string> paths)
        {
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
             {
                 await this.AddLibraryItems(paths, cancellationToken).ConfigureAwait(false);
                 if (cancellationToken.IsCancellationRequested)
                 {
                     this.Name = "Waiting..";
                     this.Description = string.Empty;
                 }
             }))
            {
                await task.Run().ConfigureAwait(false);
            }
            if (this.IsCancellationRequested)
            {
                //Reset cancellation as the next phases should finish quickly.
                //Cancelling again will still work.
                this.IsCancellationRequested = false;
            }
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                await this.AddOrUpdateMetaData(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    this.Name = "Waiting..";
                    this.Description = string.Empty;
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
            if (this.IsCancellationRequested)
            {
                //Reset cancellation as the next phases should finish quickly.
                //Cancelling again will still work.
                this.IsCancellationRequested = false;
            }
            await this.BuildHierarchies(LibraryItemStatus.Import).ConfigureAwait(false);
            await RemoveCancelledLibraryItems(this.Database).ConfigureAwait(false);
            await SetLibraryItemsStatus(this.Database, LibraryItemStatus.None).ConfigureAwait(false);
        }

        protected virtual async Task AddLibraryItems(IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var libraryPopulator = new LibraryPopulator(this.Database, this.PlaybackManager, this.Visible, transaction))
                {
                    libraryPopulator.InitializeComponent(this.Core);
                    await this.WithSubTask(libraryPopulator,
                         () => libraryPopulator.Populate(paths, cancellationToken)
                    ).ConfigureAwait(false);
                }
                transaction.Commit();
            }
        }

        protected virtual async Task AddOrUpdateMetaData(CancellationToken cancellationToken)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var metaDataPopulator = new LibraryMetaDataPopulator(this.Database, this.Visible, transaction))
                {
                    metaDataPopulator.InitializeComponent(this.Core);
                    await this.WithSubTask(metaDataPopulator,
                        () => metaDataPopulator.Populate(LibraryItemStatus.Import, cancellationToken)
                    ).ConfigureAwait(false);
                    foreach (var pair in metaDataPopulator.Warnings)
                    {
                        if (pair.Key is LibraryItem libraryItem)
                        {
                            this.Warnings.GetOrAdd(libraryItem, _libraryItem => new List<string>()).AddRange(pair.Value);
                        }
                    }
                }
                transaction.Commit();
            }
        }

        protected virtual async Task BuildHierarchies(LibraryItemStatus? status)
        {
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    await this.AddHiearchies(status, cancellationToken, transaction).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        this.Name = "Waiting..";
                        this.Description = string.Empty;
                    }
                    else
                    {
                        this.Name = "Finalizing";
                        this.Position = 0;
                        this.Count = 0;
                    }
                    transaction.Commit();
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        private async Task AddHiearchies(LibraryItemStatus? status, CancellationToken cancellationToken, ITransactionSource transaction)
        {
            using (var libraryHierarchyPopulator = new LibraryHierarchyPopulator(this.Database, this.Visible, transaction))
            {
                libraryHierarchyPopulator.InitializeComponent(this.Core);
                await this.WithSubTask(libraryHierarchyPopulator,
                    () => libraryHierarchyPopulator.Populate(status, cancellationToken)
                ).ConfigureAwait(false);
            }
        }

        protected virtual async Task RemoveHierarchies(LibraryItemStatus? status)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await RemoveHierarchies(this.Database, status, transaction).ConfigureAwait(false);
                transaction.Commit();
            }
        }

        public static Task RemoveHierarchies(IDatabaseComponent database, LibraryItemStatus? status, ITransactionSource transaction)
        {
            return RemoveHierarchies(database, null, status, transaction);
        }

        public static Task RemoveHierarchies(IDatabaseComponent database, LibraryHierarchy libraryHierarchy, LibraryItemStatus? status, ITransactionSource transaction)
        {
            return database.ExecuteAsync(database.Queries.RemoveLibraryHierarchyItems, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        if (libraryHierarchy != null)
                        {
                            parameters["libraryHierarchyId"] = libraryHierarchy.Id;
                        }
                        if (status.HasValue)
                        {
                            parameters["status"] = status;
                        }
                        break;
                }
            }, transaction);
        }

        protected virtual async Task RemoveItems(LibraryItemStatus? status)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.RemoveLibraryItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["status"] = status;
                            break;
                    }
                }, transaction).ConfigureAwait(false);
                transaction.Commit();
            }
        }

        public static IEnumerable<string> UpdateLibraryCache(ILibraryCache libraryCache, IEnumerable<LibraryItem> libraryItems, IEnumerable<string> names)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var libraryItem in libraryItems)
            {
                var cachedLibraryItem = default(LibraryItem);
                if (libraryCache.TryGet(libraryItem.Id, out cachedLibraryItem))
                {
                    if (!object.ReferenceEquals(libraryItem, cachedLibraryItem))
                    {
                        result.AddRange(MetaDataItem.Update(libraryItem, cachedLibraryItem, names));
                    }
                }
            }
            return result;
        }

        public static IEnumerable<string> UpdatePlaylistCache(IPlaylistCache playlistCache, IEnumerable<LibraryItem> libraryItems, IEnumerable<string> names)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var libraryItem in libraryItems)
            {
                var playlistItems = default(PlaylistItem[]);
                if (playlistCache.TryGetItemsByLibraryId(libraryItem.Id, out playlistItems))
                {
                    result.AddRange(MetaDataItem.Update(libraryItem, playlistItems, names));
                }
            }
            return result;
        }

        public static async Task RemoveCancelledLibraryItems(IDatabaseComponent database)
        {
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                await database.ExecuteAsync(database.Queries.RemoveCancelledLibraryItems).ConfigureAwait(false);
            }
        }

        public static async Task SetLibraryItemsStatus(IDatabaseComponent database, LibraryItemStatus status)
        {
            var query = database.QueryFactory.Build();
            query.Update.SetTable(database.Tables.LibraryItem);
            query.Update.AddColumn(database.Tables.LibraryItem.Column("Status"));
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                await database.ExecuteAsync(query, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["status"] = status;
                            break;
                    }
                }, transaction).ConfigureAwait(false);
                transaction.Commit();
            }
        }

        public static async Task UpdateLibraryItem(IDatabaseComponent database, LibraryItem libraryItem)
        {
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                await UpdateLibraryItem(database, libraryItem, transaction).ConfigureAwait(false);
                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        public static Task UpdateLibraryItem(IDatabaseComponent database, LibraryItem libraryItem, ITransactionSource transaction)
        {
            var table = database.Tables.LibraryItem;
            var builder = database.QueryFactory.Build();
            builder.Update.SetTable(table);
            builder.Update.AddColumns(table.UpdatableColumns);
            builder.Filter.AddColumns(table.PrimaryKeys);
            var query = builder.Build();
            var parameters = new ParameterHandlerStrategy(table, libraryItem).Handler;
            return database.ExecuteAsync(query, parameters, transaction);
        }

        public static async Task UpdateLibraryItem(IDatabaseComponent database, int libraryItemId, Action<LibraryItem> action)
        {
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                await UpdateLibraryItem(database, libraryItemId, action, transaction).ConfigureAwait(false);
                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        public static async Task UpdateLibraryItem(IDatabaseComponent database, int libraryItemId, Action<LibraryItem> action, ITransactionSource transaction)
        {
            var table = database.Tables.LibraryItem;
            var builder = database.QueryFactory.Build();
            builder.Output.AddColumns(table.Columns);
            builder.Source.AddTable(table);
            builder.Filter.AddColumns(table.PrimaryKeys);
            var query = builder.Build();
            var libraryItem = default(LibraryItem);
            using (var sequence = database.ExecuteAsyncEnumerator<LibraryItem>(query, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters[table.PrimaryKey] = libraryItemId;
                        break;
                }
            }, transaction))
            {
                if (await sequence.MoveNextAsync().ConfigureAwait(false))
                {
                    libraryItem = sequence.Current;
                }
            }
            action(libraryItem);
            await UpdateLibraryItem(database, libraryItem, transaction).ConfigureAwait(false);
        }

        protected override void OnDisposing()
        {
            if (this.Database != null)
            {
                this.Database.Dispose();
            }
            base.OnDisposing();
        }
    }
}
