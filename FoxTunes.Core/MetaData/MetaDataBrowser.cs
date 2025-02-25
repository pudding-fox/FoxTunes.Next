using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class MetaDataBrowser : StandardComponent, IMetaDataBrowser
    {
        public IMetaDataCache MetaDataCache { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataCache = core.Components.MetaDataCache;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.DatabaseFactory = core.Factories.Database;
            base.InitializeComponent(core);
        }

        public Task<MetaDataItem[]> GetMetaDatas(LibraryHierarchyNode libraryHierarchyNode, string name, MetaDataItemType type, int limit)
        {
            var key = new LibraryMetaDataCacheKey(libraryHierarchyNode, name, type, limit, this.LibraryHierarchyBrowser.Filter);
            return this.MetaDataCache.GetMetaDatas(key, () => this.GetMetaDatasCore(libraryHierarchyNode, name, type, limit));
        }

        private async Task<IEnumerable<MetaDataItem>> GetMetaDatasCore(LibraryHierarchyNode libraryHierarchyNode, string name, MetaDataItemType type, int limit)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = this.GetReader(database, libraryHierarchyNode, name, type, limit, transaction))
                    {
                        using (var sequence = reader.GetAsyncEnumerator())
                        {
                            var result = new List<MetaDataItem>();
                            while (await sequence.MoveNextAsync().ConfigureAwait(false))
                            {
                                result.Add(new MetaDataItem()
                                {
                                    Name = name,
                                    Type = type,
                                    Value = sequence.Current.Get<string>("Value")
                                });
                            }
                            return result;
                        }
                    }
                }
            }
        }

        private IDatabaseReader GetReader(IDatabaseComponent database, LibraryHierarchyNode libraryHierarchyNode, string name, MetaDataItemType type, int limit, ITransactionSource transaction)
        {
            var query = database.Queries.GetLibraryHierarchyMetaData(this.LibraryHierarchyBrowser.Filter, limit);
            return database.ExecuteReader(query, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["libraryHierarchyItemId"] = libraryHierarchyNode.Id;
                        parameters["name"] = name;
                        parameters["type"] = type;
                        break;
                }
            }, transaction);
        }

        public Task<MetaDataItem[]> GetMetaDatas(IEnumerable<PlaylistItem> playlistItems, string name, MetaDataItemType type, int limit)
        {
            var key = new PlaylistMetaDataCacheKey(playlistItems.ToArray(), name, type, limit, this.LibraryHierarchyBrowser.Filter);
            return this.MetaDataCache.GetMetaDatas(key, () => this.GetMetaDatasCore(playlistItems, name, type, limit));
        }

        private async Task<IEnumerable<MetaDataItem>> GetMetaDatasCore(IEnumerable<PlaylistItem> playlistItems, string name, MetaDataItemType type, int limit)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = this.GetReader(database, playlistItems, name, type, limit, transaction))
                    {
                        using (var sequence = reader.GetAsyncEnumerator())
                        {
                            var result = new List<MetaDataItem>();
                            while (await sequence.MoveNextAsync().ConfigureAwait(false))
                            {
                                result.Add(new MetaDataItem()
                                {
                                    Name = name,
                                    Type = type,
                                    Value = sequence.Current.Get<string>("Value")
                                });
                            }
                            return result;
                        }
                    }
                }
            }
        }

        private IDatabaseReader GetReader(IDatabaseComponent database, IEnumerable<PlaylistItem> playlistItems, string name, MetaDataItemType type, int limit, ITransactionSource transaction)
        {
            return database.ExecuteReader(database.Queries.GetPlaylistMetaData(playlistItems.Count(), limit), (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        var position = 0;
                        foreach (var playlistItem in playlistItems)
                        {
                            parameters["playlistItemId" + position] = playlistItem.Id;
                            position++;
                        }
                        parameters["name"] = name;
                        parameters["type"] = type;
                        break;
                }
            }, transaction);
        }
    }
}
