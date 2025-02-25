using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class RatingBehaviour : StandardBehaviour, IInvocableComponent, IUIPlaylistColumnProvider, IDatabaseInitializer
    {
        public const string ID = "2681C239-1291-4018-ACED-4933CC395FF6";

        public const string SET_LIBRARY_RATING = "AAAE";

        public const string SET_PLAYLIST_RATING = "CCCC";

        #region IPlaylistColumnProvider

        public string Id
        {
            get
            {
                return ID;
            }
        }

        public string Name
        {
            get
            {
                return "Rating Stars";
            }
        }

        public string Description
        {
            get
            {
                return null;
            }
        }

        public bool DependsOn(IEnumerable<string> names)
        {
            //ViewModel.Rating tracks updates.
            return false;
        }

        public string GetValue(PlaylistItem playlistItem)
        {
            lock (playlistItem.MetaDatas)
            {
                var metaDataItem = playlistItem.MetaDatas.FirstOrDefault(
                    _metaDataItem => string.Equals(_metaDataItem.Name, CommonStatistics.Rating)
                );
                if (metaDataItem != null)
                {
                    return metaDataItem.Value;
                }
            }
            return null;
        }

        #endregion

        #region IUIPlaylistColumnProvider

        public DataTemplate CellTemplate
        {
            get
            {
                return TemplateFactory.Template;
            }
        }

        #endregion

        #region IDatabaseInitializer

        string IDatabaseInitializer.Checksum
        {
            get
            {
                return "07E85D87-989A-46D5-9817-D7F837CE4091";
            }
        }

        void IDatabaseInitializer.InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            //IMPORTANT: When editing this function remember to change the checksum.
            if (!type.HasFlag(DatabaseInitializeType.Playlist))
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                var set = database.Set<PlaylistColumn>(transaction);
                set.Add(new PlaylistColumn()
                {
                    Name = "Rating",
                    Type = PlaylistColumnType.Plugin,
                    Sequence = 100,
                    Plugin = ID,
                    Enabled = false
                });
                transaction.Commit();
            }
        }

        #endregion

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public RatingManager RatingManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IMetaDataBrowser MetaDataBrowser { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.RatingManager = ComponentRegistry.Instance.GetComponent<RatingManager>();
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.MetaDataBrowser = core.Components.MetaDataBrowser;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
               MetaDataBehaviourConfiguration.SECTION,
               MetaDataBehaviourConfiguration.READ_POPULARIMETER_TAGS
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
                        var invocationComponents = new Dictionary<byte, InvocationComponent>();
                        for (var a = 1; a <= 5; a++)
                        {
                            var invocationComponent = new InvocationComponent(
                                InvocationComponent.CATEGORY_LIBRARY,
                                SET_LIBRARY_RATING,
                                string.Format("{0} Stars", a),
                                path: "Set Rating"
                            );
                            invocationComponents.Add((byte)a, invocationComponent);
                            yield return invocationComponent;
                        }
                        yield return new InvocationComponent(
                            InvocationComponent.CATEGORY_LIBRARY,
                            SET_LIBRARY_RATING,
                            "Reset",
                            path: "Set Rating",
                            attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                        );
                        //Don't block the menu from opening while we fetch ratings.
                        this.Dispatch(() => this.GetRating(this.LibraryManager.SelectedItem, invocationComponents));
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        var invocationComponents = new Dictionary<byte, InvocationComponent>();
                        for (var a = 1; a <= 5; a++)
                        {
                            var invocationComponent = new InvocationComponent(
                                InvocationComponent.CATEGORY_PLAYLIST,
                                SET_PLAYLIST_RATING,
                                string.Format("{0} Stars", a),
                                path: "Set Rating"
                            );
                            invocationComponents.Add((byte)a, invocationComponent);
                            yield return invocationComponent;
                        }
                        yield return new InvocationComponent(
                            InvocationComponent.CATEGORY_PLAYLIST,
                            SET_PLAYLIST_RATING,
                            "Reset",
                            path: "Set Rating",
                            attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                        );
                        //Don't block the menu from opening while we fetch ratings.
                        this.Dispatch(() => this.GetRating(this.PlaylistManager.SelectedItems, invocationComponents));
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case SET_LIBRARY_RATING:
                    return this.SetLibraryRating(component.Name);
                case SET_PLAYLIST_RATING:
                    return this.SetPlaylistRating(component.Name);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task GetRating(LibraryHierarchyNode libraryHierarchyNode, Dictionary<byte, InvocationComponent> invocationComponents)
        {
            Logger.Write(this, LogLevel.Debug, "Determining rating for library hierarchy node: {0}", libraryHierarchyNode.Id);
            var rating = default(byte);
            var ratings = await this.MetaDataBrowser.GetMetaDatas(
                libraryHierarchyNode,
                CommonStatistics.Rating,
                MetaDataItemType.Tag,
                2
            ).ConfigureAwait(false);
            switch (ratings.Length)
            {
                case 0:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have no rating.", libraryHierarchyNode.Id);
                    rating = 0;
                    break;
                case 1:
                    if (!byte.TryParse(ratings[0].Value, out rating))
                    {
                        Logger.Write(this, LogLevel.Warn, "Library hierarchy node {0} tracks have rating \"{1}\" which is in an unknown format.", libraryHierarchyNode.Id, ratings[0].Value);
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have rating {1}.", libraryHierarchyNode.Id, rating);
                    break;
                default:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have multiple ratings.", libraryHierarchyNode.Id);
                    return;
            }
            foreach (var key in invocationComponents.Keys)
            {
                var invocationComponent = invocationComponents[key];
                if (key == rating)
                {
                    invocationComponent.Attributes = (byte)(invocationComponent.Attributes | InvocationComponent.ATTRIBUTE_SELECTED);
                }
            }
        }

        protected virtual async Task GetRating(PlaylistItem[] playlistItems, Dictionary<byte, InvocationComponent> invocationComponents)
        {
            if (playlistItems.Length > ListViewExtensions.MAX_SELECTED_ITEMS)
            {
                //This would result in too many parameters.
                Logger.Write(this, LogLevel.Debug, "Cannot determining rating for {0} playlist items, max is {1}.", playlistItems.Length, ListViewExtensions.MAX_SELECTED_ITEMS);
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Determining rating for {0} playlist items.", playlistItems.Length);
            var rating = default(byte);
            var ratings = await this.MetaDataBrowser.GetMetaDatas(
                playlistItems,
                CommonStatistics.Rating,
                MetaDataItemType.Tag,
                2
            ).ConfigureAwait(false);
            switch (ratings.Length)
            {
                case 0:
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have no rating.", playlistItems.Length);
                    rating = 0;
                    break;
                case 1:
                    if (!byte.TryParse(ratings[0].Value, out rating))
                    {
                        Logger.Write(this, LogLevel.Warn, "{0} playlist items have rating \"{1}\" which is in an unknown format.", playlistItems.Length, ratings[0].Value);
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have rating {1}.", playlistItems.Length, rating);
                    break;
                default:
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have multiple ratings.", playlistItems.Length);
                    return;
            }
            foreach (var key in invocationComponents.Keys)
            {
                var invocationComponent = invocationComponents[key];
                if (key == rating)
                {
                    invocationComponent.Attributes = (byte)(invocationComponent.Attributes | InvocationComponent.ATTRIBUTE_SELECTED);
                }
            }
        }

        protected virtual Task SetLibraryRating(string name)
        {
            var rating = default(byte);
            if (string.Equals(name, "Reset", StringComparison.OrdinalIgnoreCase))
            {
                rating = 0;
            }
            else if (string.IsNullOrEmpty(name) || !byte.TryParse(name.Split(' ').FirstOrDefault(), out rating))
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
            return this.RatingManager.SetRating(libraryItems, rating);
        }

        protected virtual Task SetPlaylistRating(string name)
        {
            var rating = default(byte);
            if (string.Equals(name, "Reset", StringComparison.OrdinalIgnoreCase))
            {
                rating = 0;
            }
            else if (string.IsNullOrEmpty(name) || !byte.TryParse(name.Split(' ').FirstOrDefault(), out rating))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.RatingManager.SetRating(this.PlaylistManager.SelectedItems, rating);
        }

        private static class TemplateFactory
        {
            private static Lazy<DataTemplate> _Template = new Lazy<DataTemplate>(GetTemplate);

            public static DataTemplate Template
            {
                get
                {
                    return _Template.Value;
                }
            }

            private static DataTemplate GetTemplate()
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.Rating)))
                {
                    var template = (DataTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }
        }
    }
}
