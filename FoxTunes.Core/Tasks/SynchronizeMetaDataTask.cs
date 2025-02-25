using FoxDb;
using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class SynchronizeMetaDataTask : BackgroundTask
    {
        public const string ID = "230D384D-9B80-412B-BD08-9B6DEB0497CF";

        public SynchronizeMetaDataTask() : base(ID)
        {

        }

        public IDatabaseComponent Database { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Database = core.Factories.Database.Create();
            this.MetaDataManager = core.Managers.MetaData;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            await this.SynchronizeLibraryItems().ConfigureAwait(false);
            await this.SynchronizePlaylistItems().ConfigureAwait(false);
        }

        protected virtual async Task SynchronizeLibraryItems()
        {
            var libraryItems = default(IEnumerable<LibraryItem>);
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var queryable = this.Database.AsQueryable<LibraryItem>(transaction);
                    //TODO: Add Enum.HasFlag support to FoxDb.
                    libraryItems = queryable.Where(libraryItem => libraryItem.Flags == LibraryItemFlags.Export).ToArray();
                }
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
            if (!libraryItems.Any())
            {
                return;
            }
            await this.MetaDataManager.Synchronize(libraryItems).ConfigureAwait(false);
        }

        protected virtual async Task SynchronizePlaylistItems()
        {
            var playlistItems = default(IEnumerable<PlaylistItem>);
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_LOW, cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var queryable = this.Database.AsQueryable<PlaylistItem>(transaction);
                    //TODO: Add Enum.HasFlag support to FoxDb.
                    playlistItems = queryable.Where(playlistItem => playlistItem.Flags == PlaylistItemFlags.Export).ToArray();
                }
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
            if (!playlistItems.Any())
            {
                return;
            }
            await this.MetaDataManager.Synchronize(playlistItems).ConfigureAwait(false);
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
