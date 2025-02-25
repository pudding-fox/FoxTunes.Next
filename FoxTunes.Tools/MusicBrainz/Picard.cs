using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class Picard : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public const string PICARD = "MMMM";

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IPlaylistCache PlaylistCache { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public TextConfigurationElement Path { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.MetaDataManager = core.Managers.MetaData;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.PlaylistCache = core.Components.PlaylistCache;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                PicardConfiguration.SECTION,
                PicardConfiguration.ENABLED_ELEMENT
            );
            this.Path = this.Configuration.GetElement<TextConfigurationElement>(
                PicardConfiguration.SECTION,
                PicardConfiguration.PATH_ELEMENT
            );
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
                if (this.Enabled.Value)
                {
                    if (this.LibraryManager.SelectedItem != null)
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, PICARD, Strings.Picard_Picard, path: Strings.Picard_Path);
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, PICARD, Strings.Picard_Picard, path: Strings.Picard_Path);
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case PICARD:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.OpenLibrary();
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.OpenPlaylist();
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task OpenLibrary()
        {
            if (this.LibraryManager == null || this.LibraryManager.SelectedItem == null)
            {
                return;
            }
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(this.LibraryManager.SelectedItem);
            if (!libraryItems.Any())
            {
                return;
            }
            await this.Open(libraryItems).ConfigureAwait(false);
            await this.MetaDataManager.Rescan(libraryItems, MetaDataUpdateFlags.RefreshHierarchies).ConfigureAwait(false);
        }

        protected virtual async Task OpenPlaylist()
        {
            if (this.PlaylistManager == null || this.PlaylistManager.SelectedItems == null)
            {
                return;
            }
            //TODO: If the playlist contains duplicate tracks, will all be refreshed properly?
            var playlistItems = this.PlaylistManager.SelectedItems
                .GroupBy(playlistItem => playlistItem.FileName)
                .Select(group => group.First())
                .ToArray();
            if (!playlistItems.Any())
            {
                return;
            }
            await this.Open(playlistItems).ConfigureAwait(false);
            await this.MetaDataManager.Rescan(playlistItems, MetaDataUpdateFlags.RefreshHierarchies).ConfigureAwait(false);
        }

        protected virtual Task Open(IEnumerable<IFileData> items)
        {
            var builder = new StringBuilder();
            foreach (var item in items)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }
                builder.AppendFormat("\"{0}\"", item.FileName);
            }
            var process = Process.Start(this.Path.Value, builder.ToString());
            return process.WaitForExitAsync();
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return PicardConfiguration.GetConfigurationSections();
        }
    }
}
