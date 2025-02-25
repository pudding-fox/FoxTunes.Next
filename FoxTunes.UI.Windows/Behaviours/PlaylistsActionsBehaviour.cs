using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PlaylistsActionsBehaviour : StandardBehaviour, IInvocableComponent
    {
        public const string ADD_PLAYLIST = "QQQQ";

        public const string REMOVE_PLAYLIST = "RRRR";

        public const string MANAGE_PLAYLISTS = "SSSS";

        public const string CREATE_PLAYLIST = "AAAD";

        public PlaylistsActionsBehaviour()
        {
            Instance = this;
        }

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileActionHandlerManager FileActionHandler { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.FileActionHandler = core.Managers.FileActionHandler;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLISTS;
                yield return InvocationComponent.CATEGORY_LIBRARY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLISTS, ADD_PLAYLIST, Strings.PlaylistsActionsBehaviour_Add);
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLISTS, REMOVE_PLAYLIST, Strings.PlaylistsActionsBehaviour_Remove);
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLISTS, MANAGE_PLAYLISTS, Strings.PlaylistsActionsBehaviour_Manage, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                if (this.LibraryManager.SelectedItem != null)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, CREATE_PLAYLIST, Strings.PlaylistsActionsBehaviour_Create);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case ADD_PLAYLIST:
                    return this.AddPlaylist();
                case REMOVE_PLAYLIST:
                    return this.RemovePlaylist();
                case MANAGE_PLAYLISTS:
                    return this.ManagePlaylists();
                case CREATE_PLAYLIST:
                    return this.AddPlaylist(this.LibraryManager.SelectedItem);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task<Playlist> AddPlaylist()
        {
            var playlist = this.CreatePlaylist();
            await this.PlaylistManager.Create(playlist).ConfigureAwait(false);
            this.PlaylistManager.SelectedPlaylist = playlist;
            return playlist;
        }

        public async Task<Playlist> AddPlaylist(IEnumerable<string> paths)
        {
            var playlist = await this.AddPlaylist().ConfigureAwait(false);
            await this.FileActionHandler.RunPaths(paths, FileActionType.Playlist).ConfigureAwait(false);
            return playlist;
        }

        public async Task<Playlist> AddPlaylist(LibraryHierarchyNode libraryHierarchyNode)
        {
            var playlist = new Playlist()
            {
                Name = libraryHierarchyNode.Value,
                Enabled = true
            };
            await this.PlaylistManager.Create(playlist, libraryHierarchyNode).ConfigureAwait(false);
            this.PlaylistManager.SelectedPlaylist = playlist;
            return playlist;
        }

        public async Task<Playlist> AddPlaylist(IEnumerable<PlaylistItem> playlistItems)
        {
            var playlist = this.CreatePlaylist();
            await this.PlaylistManager.Create(playlist, playlistItems).ConfigureAwait(false);
            this.PlaylistManager.SelectedPlaylist = playlist;
            return playlist;
        }

        public async Task RemovePlaylist()
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null)
            {
                return;
            }
            var playlists = this.PlaylistBrowser.GetPlaylists();
            if (playlists.Length > 1)
            {
                var index = playlists.IndexOf(this.PlaylistManager.SelectedPlaylist);
                if (index > 0)
                {
                    index--;
                }
                else
                {
                    index++;
                }
                this.PlaylistManager.SelectedPlaylist = playlists[index];
                await this.PlaylistManager.Remove(playlist).ConfigureAwait(false);
            }
            else
            {
                await this.PlaylistManager.Remove(playlist).ConfigureAwait(false);
                await this.AddPlaylist().ConfigureAwait(false);
            }
        }

        public Task ManagePlaylists()
        {
            return Windows.Invoke(() => Windows.Registrations.Show(PlaylistManagerWindow.ID));
        }

        protected virtual Playlist CreatePlaylist()
        {
            var playlists = this.PlaylistBrowser.GetPlaylists();
            return new Playlist()
            {
                Name = Playlist.GetName(playlists),
                Enabled = true
            };
        }

        public static PlaylistsActionsBehaviour Instance { get; private set; }
    }
}
