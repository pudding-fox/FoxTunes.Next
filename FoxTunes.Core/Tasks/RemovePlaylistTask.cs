using System.Threading.Tasks;

namespace FoxTunes
{
    public class RemovePlaylistTask : PlaylistTaskBase
    {
        public RemovePlaylistTask(Playlist playlist) : base(playlist)
        {

        }

        protected override async Task OnRun()
        {
            await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            await this.RemovePlaylist().ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Removed))).ConfigureAwait(false);
        }
    }
}
