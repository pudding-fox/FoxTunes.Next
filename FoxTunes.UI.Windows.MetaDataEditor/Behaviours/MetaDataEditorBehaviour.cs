using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class MetaDataEditorBehaviour : StandardBehaviour, IInvocableComponent
    {
        public const string EDIT_METADATA = "LLLL";

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_LIBRARY;
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.LibraryManager.SelectedItem != null)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, EDIT_METADATA, Strings.MetaDataEditorBehaviour_Tag);
                }
                if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, EDIT_METADATA, Strings.MetaDataEditorBehaviour_Tag);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case EDIT_METADATA:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.EditLibrary();
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.EditPlaylist();
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task EditLibrary()
        {
            if (this.LibraryManager == null || this.LibraryManager.SelectedItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(this.LibraryManager.SelectedItem);
            if (!libraryItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Edit(libraryItems);
        }

        public Task EditPlaylist()
        {
            if (this.PlaylistManager == null || this.PlaylistManager.SelectedItems == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            //TODO: If the playlist contains duplicate tracks, will all be refreshed properly?
            var playlistItems = this.PlaylistManager.SelectedItems
                .GroupBy(playlistItem => playlistItem.FileName)
                .Select(group => group.First())
                .ToArray();
            if (!playlistItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Edit(playlistItems);
        }

        public Task Edit(IFileData[] fileDatas)
        {
            return Windows.Invoke(() =>
            {
                var window = new MetaDataEditorWindow()
                {
                    ShowActivated = true
                };
                window.Show(fileDatas);
            });
        }
    }
}
