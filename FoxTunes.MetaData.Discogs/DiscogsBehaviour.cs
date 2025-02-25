using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class DiscogsBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public const string LOOKUP_TAGS = "LLMN";

        public const string LOOKUP_ARTWORK = "LLMM";

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public BooleanConfigurationElement AutoLookup { get; private set; }

        public BooleanConfigurationElement WriteTags { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.OnDemandMetaDataProvider = core.Components.OnDemandMetaDataProvider;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ReportEmitter = core.Components.ReportEmitter;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.ENABLED
            );
            this.AutoLookup = this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.AUTO_LOOKUP
            );
            this.WriteTags = this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.WRITE_TAGS
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
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, LOOKUP_TAGS, Strings.DiscogsBehaviour_FetchTags, path: Strings.DiscogsBehaviourConfiguration_Section);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, LOOKUP_ARTWORK, Strings.DiscogsBehaviour_FetchArtwork, path: Strings.DiscogsBehaviourConfiguration_Section);
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, LOOKUP_TAGS, Strings.DiscogsBehaviour_FetchTags, path: Strings.DiscogsBehaviourConfiguration_Section);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, LOOKUP_ARTWORK, Strings.DiscogsBehaviour_FetchArtwork, path: Strings.DiscogsBehaviourConfiguration_Section);
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case LOOKUP_TAGS:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.FetchTagsLibrary(MetaDataUpdateType.User);
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.FetchTagsPlaylist(MetaDataUpdateType.User);
                    }
                    break;
                case LOOKUP_ARTWORK:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.FetchArtworkLibrary(MetaDataUpdateType.User);
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.FetchArtworkPlaylist(MetaDataUpdateType.User);
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task FetchTagsLibrary(MetaDataUpdateType updateType)
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
            var releaseLookups = await this.FetchTags(libraryItems, updateType).ConfigureAwait(false);
            this.OnReport(releaseLookups);
        }

        public async Task FetchTagsPlaylist(MetaDataUpdateType updateType)
        {
            if (this.PlaylistManager.SelectedItems == null)
            {
                return;
            }
            var playlistItems = this.PlaylistManager.SelectedItems.ToArray();
            if (!playlistItems.Any())
            {
                return;
            }
            var releaseLookups = await this.FetchTags(playlistItems, updateType).ConfigureAwait(false);
            this.OnReport(releaseLookups);
        }

        public async Task<IEnumerable<Discogs.ReleaseLookup>> FetchTags(IEnumerable<IFileData> fileDatas, MetaDataUpdateType updateType)
        {
            var releaseLookups = Discogs.ReleaseLookup.FromFileDatas(fileDatas).ToArray();
            if (releaseLookups.Any())
            {
                using (var task = new DiscogsFetchTagsTask(releaseLookups, updateType))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
            return releaseLookups;
        }

        public async Task FetchArtworkLibrary(MetaDataUpdateType updateType)
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
            var releaseLookups = await this.FetchReleases(libraryItems, updateType).ConfigureAwait(false);
            this.OnReport(releaseLookups);
            await this.OnDemandMetaDataProvider.SetMetaData(
                new OnDemandMetaDataRequest(
                    CommonImageTypes.FrontCover,
                    MetaDataItemType.Image,
                    updateType
                ),
                this.GetMetaDataValues(releaseLookups, CommonImageTypes.FrontCover, true)
            ).ConfigureAwait(false);
        }

        public async Task FetchArtworkPlaylist(MetaDataUpdateType updateType)
        {
            if (this.PlaylistManager.SelectedItems == null)
            {
                return;
            }
            var playlistItems = this.PlaylistManager.SelectedItems.ToArray();
            if (!playlistItems.Any())
            {
                return;
            }
            var releaseLookups = await this.FetchReleases(playlistItems, updateType).ConfigureAwait(false);
            this.OnReport(releaseLookups);
            await this.OnDemandMetaDataProvider.SetMetaData(
                new OnDemandMetaDataRequest(
                    CommonImageTypes.FrontCover,
                    MetaDataItemType.Image,
                    updateType
                ),
                this.GetMetaDataValues(releaseLookups, CommonImageTypes.FrontCover, true)
            ).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Discogs.ReleaseLookup>> FetchReleases(IEnumerable<IFileData> fileDatas, MetaDataUpdateType updateType)
        {
            var releaseLookups = Discogs.ReleaseLookup.FromFileDatas(fileDatas).ToArray();
            if (releaseLookups.Any())
            {
                using (var task = new DiscogsFetchArtworkTask(releaseLookups, updateType))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
            return releaseLookups;
        }

        public OnDemandMetaDataValues GetMetaDataValues(IEnumerable<Discogs.ReleaseLookup> releaseLookups, string name, bool writeToFiles)
        {
            var values = new List<OnDemandMetaDataValue>();
            foreach (var releaseLookup in releaseLookups)
            {
                if (releaseLookup.Status != Discogs.ReleaseLookupStatus.Complete)
                {
                    continue;
                }
                var value = default(string);
                if (!releaseLookup.MetaData.TryGetValue(name, out value))
                {
                    continue;
                }
                foreach (var fileData in releaseLookup.FileDatas)
                {
                    values.Add(new OnDemandMetaDataValue(fileData, value));
                }
            }
            var flags = MetaDataUpdateFlags.None;
            if (this.WriteTags.Value && writeToFiles)
            {
                flags |= MetaDataUpdateFlags.WriteToFiles;
            }
            return new OnDemandMetaDataValues(values, flags);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return DiscogsBehaviourConfiguration.GetConfigurationSections();
        }

        protected virtual void OnReport(IEnumerable<Discogs.ReleaseLookup> releaseLookups)
        {
            var report = new ReleaseLookupReport(releaseLookups.ToArray());
            report.InitializeComponent(this.Core);
            this.ReportEmitter.Send(report);
        }
    }
}