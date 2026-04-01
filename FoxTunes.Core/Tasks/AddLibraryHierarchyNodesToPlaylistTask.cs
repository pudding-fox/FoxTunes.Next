using FoxDb.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddLibraryHierarchyNodesToPlaylistTask : PlaylistTaskBase
    {
        public AddLibraryHierarchyNodesToPlaylistTask(Playlist playlist, int sequence, LibraryHierarchy libraryHierarchy, string filter, bool clear, bool visible = true)
            : base(playlist, sequence)
        {
            this.LibraryHierarchy = libraryHierarchy;
            this.Filter = filter;
            this.Clear = clear;
            this._Visible = visible;
        }

        public LibraryHierarchy LibraryHierarchy { get; private set; }

        public string Filter { get; private set; }

        public bool Clear { get; private set; }

        private bool _Visible { get; set; }

        protected override async Task OnRun()
        {
            if (this.Clear)
            {
                await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            }
            await this.AddPlaylistItems().ConfigureAwait(false);
            if (!this.Clear)
            {
                await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
            }
            await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
        }

        private async Task AddPlaylistItems()
        {
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    await this.AddPlaylistItems(this.Database.Queries.AddLibraryHierarchyNodesToPlaylist(this.Filter, this.Sort.Value), transaction).ConfigureAwait(false);
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        private async Task AddPlaylistItems(IDatabaseQuery query, ITransactionSource transaction)
        {
            var count = await this.Database.ExecuteScalarAsync<int>(query, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["playlistId"] = this.Playlist.Id;
                        parameters["libraryHierarchyId"] = this.LibraryHierarchy.Id;
                        parameters["sequence"] = this.Sequence;
                        parameters["status"] = PlaylistItemStatus.Import;
                        break;
                }
            }, transaction).ConfigureAwait(false);
            this.Sequence += count;
            this.Offset += count;
        }
    }
}
