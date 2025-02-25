using FoxDb.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class CreateSmartPlaylistTask : PlaylistTaskBase
    {
        public CreateSmartPlaylistTask(Playlist playlist, string filter, string sort, int limit) : base(playlist, 0)
        {
            this.Filter = filter;
            this.Sort = sort;
            this.Limit = limit;
        }

        public string Filter { get; private set; }

        new public string Sort { get; private set; }

        public int Limit { get; private set; }

        protected override async Task OnRun()
        {
            await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            await this.AddPlaylistItems().ConfigureAwait(false);
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
                    await this.AddPlaylistItems(this.Database.Queries.AddSearchToPlaylist(this.Filter, this.Sort, this.Limit), transaction).ConfigureAwait(false);
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
