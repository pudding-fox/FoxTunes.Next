using System.Threading.Tasks;

namespace FoxTunes
{
    public class SortPlaylistTask : PlaylistTaskBase
    {
        public SortPlaylistTask(Playlist playlist, PlaylistColumn playlistColumn, bool descending) : base(playlist)
        {
            this.PlaylistColumn = playlistColumn;
            this.Descending = descending;
        }

        public PlaylistColumn PlaylistColumn { get; private set; }

        public bool Descending { get; private set; }

        public int Changes { get; private set; }

        protected override async Task OnRun()
        {
            this.Changes = await this.SortItems(this.PlaylistColumn, this.Descending).ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            if (this.Changes != 0)
            {
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
            }
        }
    }
}