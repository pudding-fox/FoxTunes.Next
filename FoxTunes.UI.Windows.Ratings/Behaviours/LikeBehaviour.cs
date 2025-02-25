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
    [WindowsUserInterfaceDependency]
    public class LikeBehaviour : StandardBehaviour, IInvocableComponent, IUIPlaylistColumnProvider, IDatabaseInitializer
    {
        public const string ID = "E85038A2-A724-4046-BA64-3A5CEC6976AC";

        public const string SET_LIBRARY_LIKE = "AAAE";

        public const string SET_PLAYLIST_LIKE = "CCCC";

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
                return "Like";
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
            //ViewModel.Like tracks updates.
            return false;
        }

        public string GetValue(PlaylistItem playlistItem)
        {
            lock (playlistItem.MetaDatas)
            {
                var metaDataItem = playlistItem.MetaDatas.FirstOrDefault(
                    _metaDataItem => string.Equals(_metaDataItem.Name, CommonStatistics.Like)
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
                return "92A00CED-B3B5-42A1-BF1B-7EC71BB530CB";
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
                    Name = "Like",
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

        public LikeManager LikeManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IMetaDataBrowser MetaDataBrowser { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.LikeManager = ComponentRegistry.Instance.GetComponent<LikeManager>();
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
                        yield return new InvocationComponent(
                            InvocationComponent.CATEGORY_LIBRARY,
                            SET_LIBRARY_LIKE,
                            "Like",
                            attributes: this.GetLibraryLike(this.LibraryManager.SelectedItem).Result ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                        );
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        yield return new InvocationComponent(
                            InvocationComponent.CATEGORY_PLAYLIST,
                            SET_PLAYLIST_LIKE,
                            "Like",
                            attributes: this.GetPlaylistLike(this.PlaylistManager.SelectedItems).Result ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                        );
                    }
                }
            }
        }

        protected virtual async Task<bool> GetLibraryLike(LibraryHierarchyNode libraryHierarchyNode)
        {
            var likes = await this.MetaDataBrowser.GetMetaDatas(
                libraryHierarchyNode,
                CommonStatistics.Like,
                MetaDataItemType.Tag,
                2
            ).ConfigureAwait(false);
            switch (likes.Length)
            {
                case 0:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have no likes.", libraryHierarchyNode.Id);
                    return false;
                case 1:
                    var like = default(bool);
                    if (!bool.TryParse(likes[0].Value, out like))
                    {
                        Logger.Write(this, LogLevel.Warn, "Library hierarchy node {0} tracks have like \"{1}\" which is in an unknown format.", libraryHierarchyNode.Id, likes[0].Value);
                        return false;
                    }
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have like {1}.", libraryHierarchyNode.Id, like);
                    return like;
                default:
                    Logger.Write(this, LogLevel.Debug, "Library hierarchy node {0} tracks have ambiguous likes.", libraryHierarchyNode.Id);
                    return false;
            }
        }

        protected virtual async Task<bool> GetPlaylistLike(PlaylistItem[] playlistItems)
        {
            var likes = await this.MetaDataBrowser.GetMetaDatas(
                playlistItems,
                CommonStatistics.Like,
                MetaDataItemType.Tag,
                2
            ).ConfigureAwait(false);
            switch (likes.Length)
            {
                case 0:
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have no likes.", playlistItems.Length);
                    return false;
                case 1:
                    var like = default(bool);
                    if (!bool.TryParse(likes[0].Value, out like))
                    {
                        Logger.Write(this, LogLevel.Warn, "{0} playlist items have like \"{1}\" which is in an unknown format.", playlistItems.Length, likes[0].Value);
                        return false;
                    }
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have like {1}.", playlistItems.Length, like);
                    return like;
                default:
                    Logger.Write(this, LogLevel.Debug, "{0} playlist items have ambiguous likes.", playlistItems.Length);
                    return false;
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case SET_LIBRARY_LIKE:
                    return this.SetLibraryLike();
                case SET_PLAYLIST_LIKE:
                    return this.SetPlaylistLike();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task SetLibraryLike()
        {
            var like = await this.GetLibraryLike(this.LibraryManager.SelectedItem).ConfigureAwait(false);
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(this.LibraryManager.SelectedItem);
            if (!libraryItems.Any())
            {
                return;
            }
            await this.LikeManager.SetLike(libraryItems, !like).ConfigureAwait(false);
        }

        protected virtual async Task SetPlaylistLike()
        {
            var like = await this.GetPlaylistLike(this.PlaylistManager.SelectedItems).ConfigureAwait(false);
            await this.LikeManager.SetLike(this.PlaylistManager.SelectedItems, !like).ConfigureAwait(false);
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
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.Like)))
                {
                    var template = (DataTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }
        }
    }
}
