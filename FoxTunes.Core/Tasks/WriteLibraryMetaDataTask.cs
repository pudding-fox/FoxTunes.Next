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
    public class WriteLibraryMetaDataTask : BackgroundTask
    {
        const MetaDataItemType META_DATA_TYPE = MetaDataItemType.Tag | MetaDataItemType.Image;

        public const string ID = "3EDED881-5AFD-46B2-AD53-78290E535F2E";

        public WriteLibraryMetaDataTask(IEnumerable<LibraryItem> libraryItems, IEnumerable<string> names, MetaDataUpdateType updateType, MetaDataUpdateFlags flags) : base(ID)
        {
            this.LibraryItems = libraryItems;
            this.Names = names;
            this.UpdateType = updateType;
            this.Flags = flags;
            this.Errors = new Dictionary<LibraryItem, IList<string>>();
        }

        public override bool Visible
        {
            get
            {
                return this.LibraryItems.Count() > 1;
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

        public IEnumerable<string> Names { get; private set; }

        public MetaDataUpdateType UpdateType { get; private set; }

        public MetaDataUpdateFlags Flags { get; private set; }

        public IDictionary<LibraryItem, IList<string>> Errors { get; private set; }

        public IDatabaseComponent Database { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public IPlaylistCache PlaylistCache { get; private set; }

        public ILibraryCache LibraryCache { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Database = core.Factories.Database.Create();
            this.MetaDataManager = core.Managers.MetaData;
            this.PlaylistCache = core.Components.PlaylistCache;
            this.LibraryCache = core.Components.LibraryCache;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override async Task OnStarted()
        {
            if (this.Visible)
            {
                this.Name = "Saving meta data";
                this.Position = 0;
                this.Count = this.LibraryItems.Count();
            }
            await base.OnStarted().ConfigureAwait(false);
            //We don't need a lock for this so not waiting for OnRun().
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            names.AddRange(LibraryTaskBase.UpdateLibraryCache(this.LibraryCache, this.LibraryItems, this.Names));
            names.AddRange(LibraryTaskBase.UpdatePlaylistCache(this.PlaylistCache, this.LibraryItems, this.Names));
        }

        protected override async Task OnRun()
        {
            var position = 0;
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                foreach (var libraryItem in this.LibraryItems)
                {
                    if (this.IsCancellationRequested)
                    {
                        break;
                    }

                    if (this.Visible)
                    {
                        this.Description = Path.GetFileName(libraryItem.FileName);
                        this.Position = position;
                    }

                    await this.WriteLibraryMetaData(libraryItem).ConfigureAwait(false);

                    libraryItem.Status = LibraryItemStatus.Import;
                    await LibraryTaskBase.UpdateLibraryItem(this.Database, libraryItem).ConfigureAwait(false);

                    if (this.Flags.HasFlag(MetaDataUpdateFlags.WriteToFiles))
                    {
                        if (!await this.MetaDataManager.Synchronize(new[] { libraryItem }, this.Names.ToArray()).ConfigureAwait(false))
                        {
                            this.AddError(libraryItem, string.Format("Failed to write meta data to file \"{0}\". We will try again later.", libraryItem.FileName));
                        }
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
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataUpdated, new MetaDataUpdatedSignalState(this.LibraryItems, this.Names, this.UpdateType))).ConfigureAwait(false);
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

                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }

        protected virtual void AddError(LibraryItem libraryItem, string message)
        {
            var errors = default(IList<string>);
            if (!this.Errors.TryGetValue(libraryItem, out errors))
            {
                errors = new List<string>();
                this.Errors.Add(libraryItem, errors);
            }
            errors.Add(message);
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
