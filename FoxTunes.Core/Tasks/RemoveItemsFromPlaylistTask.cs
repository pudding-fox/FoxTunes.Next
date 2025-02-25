using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class RemoveItemsFromPlaylistTask : PlaylistTaskBase
    {
        public RemoveItemsFromPlaylistTask(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
            : base(playlist)
        {
            this.PlaylistItems = playlistItems;
        }

        public IEnumerable<PlaylistItem> PlaylistItems { get; private set; }

        protected override Task OnRun()
        {
            return this.RemoveItems(this.PlaylistItems);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
        }
    }
}
