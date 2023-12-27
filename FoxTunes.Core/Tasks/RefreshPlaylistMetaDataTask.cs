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
    public class RefreshPlaylistMetaDataTask : BackgroundTask
    {
        const MetaDataItemType META_DATA_TYPE = MetaDataItemType.Tag | MetaDataItemType.Image;

        public const string ID = "8DB1257E-5854-4F8F-BAE3-D59A45DEE998";

        public RefreshPlaylistMetaDataTask(IEnumerable<PlaylistItem> playlistItems, MetaDataUpdateType updateType)
            : base(ID)
        {
            this.PlaylistItems = playlistItems;
            this.UpdateType = updateType;
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

        public IEnumerable<PlaylistItem> PlaylistItems { get; private set; }

        public MetaDataUpdateType UpdateType { get; private set; }

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
            this.Count = this.PlaylistItems.Count();
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            var position = 0;
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                var metaDataSource = this.MetaDataSourceFactory.Create();
                foreach (var playlistItem in this.PlaylistItems)
                {
                    this.Description = Path.GetFileName(playlistItem.FileName);
                    this.Position = position;

                    if (!File.Exists(playlistItem.FileName))
                    {
                        Logger.Write(this, LogLevel.Debug, "File \"{0}\" no longer exists: Cannot refresh.", playlistItem.FileName);
                        continue;
                    }

                    var metaDatas = await metaDataSource.GetMetaData(playlistItem.FileName).ConfigureAwait(false);
                    MetaDataItem.Update(metaDatas, playlistItem.MetaDatas, null);

                    if (playlistItem.LibraryItem_Id.HasValue)
                    {
                        await this.WriteLibraryMetaData(playlistItem).ConfigureAwait(false);
                        await LibraryTaskBase.UpdateLibraryItem(
                            this.Database,
                            playlistItem.LibraryItem_Id.Value,
                            libraryItem => libraryItem.Status = LibraryItemStatus.Import
                        ).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.WritePlaylistMetaData(playlistItem).ConfigureAwait(false);
                    }

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
            names.AddRange(PlaylistTaskBase.UpdateLibraryCache(this.LibraryCache, this.PlaylistItems, null));
            names.AddRange(PlaylistTaskBase.UpdatePlaylistCache(this.PlaylistCache, this.PlaylistItems, null));
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataUpdated, new MetaDataUpdatedSignalState(this.PlaylistItems, names, this.UpdateType))).ConfigureAwait(false);
        }

        private async Task WritePlaylistMetaData(PlaylistItem playlistItem)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.ClearPlaylistMetaDataItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["itemId"] = playlistItem.Id;
                            parameters["type"] = META_DATA_TYPE;
                            break;
                    }
                }, transaction).ConfigureAwait(false);

                using (var writer = new MetaDataWriter(this.Database, this.Database.Queries.AddPlaylistMetaDataItem, transaction))
                {
                    await writer.Write(
                        playlistItem.Id,
                        playlistItem.MetaDatas,
                        metaDataItem => META_DATA_TYPE.HasFlag(metaDataItem.Type)
                    ).ConfigureAwait(false);
                }

                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        private async Task WriteLibraryMetaData(PlaylistItem playlistItem)
        {
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                await this.Database.ExecuteAsync(this.Database.Queries.ClearLibraryMetaDataItems, (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["itemId"] = playlistItem.LibraryItem_Id.Value;
                            parameters["type"] = META_DATA_TYPE;
                            break;
                    }
                }, transaction).ConfigureAwait(false);

                using (var writer = new MetaDataWriter(this.Database, this.Database.Queries.AddLibraryMetaDataItem, transaction))
                {
                    await writer.Write(
                        playlistItem.LibraryItem_Id.Value,
                        playlistItem.MetaDatas,
                        metaDataItem => META_DATA_TYPE.HasFlag(metaDataItem.Type)
                    ).ConfigureAwait(false);
                }

                await LibraryTaskBase.UpdateLibraryItem(
                    this.Database,
                    playlistItem.LibraryItem_Id.Value,
                    libraryItem => libraryItem.ImportDate = DateTimeHelper.ToString(DateTime.UtcNow.AddSeconds(30)),
                    transaction
                ).ConfigureAwait(false);

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
