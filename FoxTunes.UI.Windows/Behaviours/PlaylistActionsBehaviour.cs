using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PlaylistActionsBehaviour : StandardBehaviour, IInvocableComponent
    {

        public const string REMOVE_PLAYLIST_ITEMS = "AAAA";

        public const string CROP_PLAYLIST_ITEMS = "AAAB";

        public const string LOCATE_PLAYLIST_ITEMS = "AAAC";

        public const string ADD_FILES = "ZZAA";

        public const string ADD_FOLDERS = "ZZAB";

        public const string SELECTION_FOLLOW_PLAYBACK = "ZZAC";

        public const string SETTINGS = "ZZZZ";

        public static readonly string MyMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        public PlaylistActionsBehaviour()
        {
            Instance = this;
        }

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IFileActionHandlerManager FileActionHandlerManager { get; private set; }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public IOutput Output { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement SelectionFollowsPlayback { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaylistManager = core.Managers.Playlist;
            this.FileActionHandlerManager = core.Managers.FileActionHandler;
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            this.Output = core.Components.Output;
            this.Configuration = core.Components.Configuration;
            this.SelectionFollowsPlayback = this.Configuration.GetElement<BooleanConfigurationElement>(
                SelectionFollowsPlaybackBehaviourConfiguration.SECTION,
                SelectionFollowsPlaybackBehaviourConfiguration.SELECTION_FOLLOWS_PLAYBACK
            );
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
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, ADD_FILES, Strings.PlaylistActionsBehaviour_AddFiles, path: Strings.PlaylistActionsBehaviour_Playlist);
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, ADD_FOLDERS, Strings.PlaylistActionsBehaviour_AddFolders, path: Strings.PlaylistActionsBehaviour_Playlist);
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SELECTION_FOLLOW_PLAYBACK, Strings.PlaylistActionsBehaviour_SelectionFollowsPlayback, path: Strings.PlaylistActionsBehaviour_Playlist, attributes: this.SelectionFollowsPlayback.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE);
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SETTINGS, Strings.PlaylistActionsBehaviour_Settings, path: Strings.PlaylistActionsBehaviour_Playlist);
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
                case ADD_FILES:
                    return this.AddFiles();
                case ADD_FOLDERS:
                    return this.AddFolders();
                case SELECTION_FOLLOW_PLAYBACK:
                    this.SelectionFollowsPlayback.Value = !this.SelectionFollowsPlayback.Value;
                    break;
                case SETTINGS:
                    return this.Settings();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task Add(Playlist playlist, IEnumerable<string> paths, bool clear)
        {
            if (clear && Playlist.CanClear(playlist))
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
            var fileNames = this.PlaylistManager.SelectedItems.Select(
                playlistItem => playlistItem.FileName
            ).ToArray();
            this.FileSystemBrowser.Select(fileNames);
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task AddFiles()
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var directoryName = default(string);
            if (!string.IsNullOrEmpty(BrowseOptions.PreviousFolderName))
            {
                directoryName = BrowseOptions.PreviousFolderName;
            }
            else
            {
                directoryName = MyMusic;
            }
            var options = new BrowseOptions(
                Strings.PlaylistActionsBehaviour_AddFiles,
                directoryName,
                new[]
                {
                    new BrowseFilter(Strings.PlaylistActionsBehaviour_All, this.Output.SupportedExtensions)
                },
                BrowseFlags.File | BrowseFlags.Multiselect
            );
            var result = this.FileSystemBrowser.Browse(options);
            if (!result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Add(
                playlist,
                result.Paths,
                false
            );
        }

        public Task AddFolders()
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            if (playlist == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var directoryName = default(string);
            if (!string.IsNullOrEmpty(BrowseOptions.PreviousFolderName))
            {
                directoryName = BrowseOptions.PreviousFolderName;
            }
            else
            {
                directoryName = MyMusic;
            }
            var options = new BrowseOptions(
                Strings.PlaylistActionsBehaviour_AddFolders,
                directoryName,
                Enumerable.Empty<BrowseFilter>(),
                BrowseFlags.Folder | BrowseFlags.Multiselect
            );
            var result = this.FileSystemBrowser.Browse(options);
            if (!result.Success)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Add(
                playlist,
                result.Paths,
                false
            );
        }

        public Task Settings()
        {
            return Windows.ShowDialog<PlaylistSettingsDialog>(this.Core, Strings.General_Settings);
        }

        public static PlaylistActionsBehaviour Instance { get; private set; }
    }
}
