using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddPlaylistTask : PlaylistTaskBase
    {
        public AddPlaylistTask(Playlist playlist) : base(playlist)
        {

        }

        public AddPlaylistTask(Playlist playlist, IEnumerable<string> paths) : this(playlist)
        {
            this.Paths = paths;
        }

        public AddPlaylistTask(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode) : this(playlist)
        {
            this.LibraryHierarchyNode = libraryHierarchyNode;
        }

        public AddPlaylistTask(Playlist playlist, IEnumerable<PlaylistItem> playlistItems) : this(playlist)
        {
            this.PlaylistItems = playlistItems;
        }

        public IEnumerable<string> Paths { get; private set; }

        public LibraryHierarchyNode LibraryHierarchyNode { get; private set; }

        public IEnumerable<PlaylistItem> PlaylistItems { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            await this.AddPlaylist().ConfigureAwait(false);
            if (this.Paths != null)
            {
                await this.AddPaths(this.Paths).ConfigureAwait(false);
            }
            else if (this.LibraryHierarchyNode != null)
            {
                await this.AddPlaylistItems(this.LibraryHierarchyNode, this.LibraryHierarchyBrowser.Filter).ConfigureAwait(false);
                await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
            }
            else if (this.PlaylistItems != null && this.PlaylistItems.Any())
            {
                await this.AddPlaylistItems(this.PlaylistItems).ConfigureAwait(false);
                await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
            }
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Added))).ConfigureAwait(false);
        }
    }
}
