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
    public class RefreshLibraryMetaDataTask : BackgroundTask
    {
        const MetaDataItemType META_DATA_TYPE = MetaDataItemType.Tag | MetaDataItemType.Image;

        public const string ID = "C595F0CE-9DF7-43F2-9B1A-32D2F2C97F22";

        public RefreshLibraryMetaDataTask(IEnumerable<LibraryItem> libraryItems) : base(ID)
        {
            this.LibraryItems = libraryItems;
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

        public IEnumerable<LibraryItem> LibraryItems { get; private set; }

        public IDatabaseComponent Database { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public IPlaylistCache PlaylistCache { get; private set; }

        public ILibraryCache LibraryCache { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Database = core.Factories.Database.Create();
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            this.PlaylistCache = core.Components.PlaylistCache;
            this.LibraryCache = core.Components.LibraryCache;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override async Task OnStarted()
        {
            this.Name = "Refreshing meta data";
            this.Position = 0;
            this.Count = this.LibraryItems.Count();
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            var position = 0;
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                var metaDataSource = this.MetaDataSourceFactory.Create();
                foreach (var libraryItem in this.LibraryItems)
                {
                    this.Description = Path.GetFileName(libraryItem.FileName);
                    this.Position = position;

                    if (!File.Exists(libraryItem.FileName))
                    {
                        Logger.Write(this, LogLevel.Debug, "File \"{0}\" no longer exists: Cannot refresh.", libraryItem.FileName);
                        continue;
                    }

                    var metaDatas = await metaDataSource.GetMetaData(libraryItem.FileName).ConfigureAwait(false);
                    MetaDataItem.Update(metaDatas, libraryItem.MetaDatas, null);

                    await this.WriteLibraryMetaData(libraryItem).ConfigureAwait(false);

                    libraryItem.Status = LibraryItemStatus.Import;
                    await LibraryTaskBase.UpdateLibraryItem(this.Database, libraryItem).ConfigureAwait(false);

                    position++;
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            //We don't need a lock for this so not performing in OnRun().
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            names.AddRange(LibraryTaskBase.UpdateLibraryCache(this.LibraryCache, this.LibraryItems, null));
            names.AddRange(LibraryTaskBase.UpdatePlaylistCache(this.PlaylistCache, this.LibraryItems, null));
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataUpdated, new MetaDataUpdatedSignalState(this.LibraryItems, names, MetaDataUpdateType.System))).ConfigureAwait(false);
        }

        private async Task WriteLibraryMetaData(LibraryItem libraryItem)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.ClearLibraryMetaDataItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["itemId"] = libraryItem.Id;
                            parameters["type"] = META_DATA_TYPE;
                            break;
                    }
                }, transaction).ConfigureAwait(false);

                using (var writer = new MetaDataWriter(this.Database, this.Database.Queries.AddLibraryMetaDataItem, transaction))
                {
                    await writer.Write(
                        libraryItem.Id,
                        libraryItem.MetaDatas,
                        metaDataItem => META_DATA_TYPE.HasFlag(metaDataItem.Type)
                    ).ConfigureAwait(false);
                }

                libraryItem.ImportDate = DateTimeHelper.ToString(DateTime.UtcNow.AddSeconds(30));
                await LibraryTaskBase.UpdateLibraryItem(this.Database, libraryItem, transaction).ConfigureAwait(false);

                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
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
