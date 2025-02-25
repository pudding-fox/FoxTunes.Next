using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class PlaylistActionsBehaviour : StandardBehaviour, IInvocableComponent
    {
        public const string REMOVE_PLAYLIST_ITEMS = "AAAA";

        public const string CROP_PLAYLIST_ITEMS = "AAAB";

        public const string LOCATE_PLAYLIST_ITEMS = "AAAC";

        public PlaylistActionsBehaviour()
        {
            Instance = this;
        }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileActionHandlerManager FileActionHandlerManager { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.FileActionHandlerManager = core.Managers.FileActionHandler;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, REMOVE_PLAYLIST_ITEMS, Strings.PlaylistActionsBehaviour_Remove);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, CROP_PLAYLIST_ITEMS, Strings.PlaylistActionsBehaviour_Crop);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, LOCATE_PLAYLIST_ITEMS, Strings.PlaylistActionsBehaviour_Locate);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case REMOVE_PLAYLIST_ITEMS:
                    return this.RemovePlaylistItems();
                case CROP_PLAYLIST_ITEMS:
                    return this.CropPlaylistItems();
                case LOCATE_PLAYLIST_ITEMS:
                    return this.LocatePlaylistItems();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task Add(Playlist playlist, IEnumerable<string> paths, bool clear)
        {
            if (clear)
            {
                await this.PlaylistManager.Clear(playlist).ConfigureAwait(false);
            }
            if (this.PlaylistManager.SelectedPlaylist != playlist)
            {
                this.PlaylistManager.SelectedPlaylist = playlist;
            }
            await this.FileActionHandlerManager.RunPaths(paths, FileActionType.Playlist).ConfigureAwait(false);
        }

        public Task Add(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode, bool clear)
        {
            return this.PlaylistManager.Add(playlist, libraryHierarchyNode, clear);
        }

        public Task Add(Playlist playlist, IEnumerable<PlaylistItem> playlistItems, bool clear)
        {
            return this.PlaylistManager.Add(playlist, playlistItems, clear);
        }

        public Task RemovePlaylistItems()
        {
            return this.PlaylistManager.Remove(this.PlaylistManager.SelectedPlaylist, this.PlaylistManager.SelectedItems);
        }

        public Task CropPlaylistItems()
        {
            return this.PlaylistManager.Crop(this.PlaylistManager.SelectedPlaylist, this.PlaylistManager.SelectedItems);
        }

        public Task LocatePlaylistItems()
        {
            foreach (var item in this.PlaylistManager.SelectedItems)
            {
                this.FileSystemBrowser.Select(item.FileName);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public static PlaylistActionsBehaviour Instance { get; private set; }
    }
}
