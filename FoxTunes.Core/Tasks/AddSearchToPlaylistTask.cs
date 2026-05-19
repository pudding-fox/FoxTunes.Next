using FoxDb.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddSearchToPlaylistTask : PlaylistTaskBase
    {
        public AddSearchToPlaylistTask(Playlist playlist, int sequence, string filter, bool clear) : base(playlist, sequence)
        {
            this.Filter = filter;
            this.Clear = clear;
        }

        public string Filter { get; private set; }

        public bool Clear { get; private set; }

        protected override async Task OnRun()
        {
            if (this.Clear)
            {
                await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            }
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
                    await this.AddPlaylistItems(this.Database.Queries.AddSearchToPlaylist(this.Filter, this.Sort.Value, 0), transaction).ConfigureAwait(false);
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
