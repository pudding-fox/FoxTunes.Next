using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class MetaDataProviderManager : StandardManager, IMetaDataProviderManager
    {
        public MetaDataProviderManager()
        {
            this._Providers = new Lazy<IDictionary<MetaDataProviderType, IMetaDataProvider>>(
                () => ComponentRegistry.Instance.GetComponents<IMetaDataProvider>().ToDictionary(
                    component => component.Type
                )
            );
        }

        public Lazy<IDictionary<MetaDataProviderType, IMetaDataProvider>> _Providers { get; private set; }

        public IMetaDataProviderCache MetaDataProviderCache { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataProviderCache = core.Components.MetaDataProviderCache;
            this.DatabaseFactory = core.Factories.Database;
            base.InitializeComponent(core);
        }

        public IMetaDataProvider GetProvider(MetaDataProvider metaDataProvider)
        {
            var provider = default(IMetaDataProvider);
            if (!this._Providers.Value.TryGetValue(metaDataProvider.Type, out provider))
            {
                return null;
            }
            return provider;
        }

        public MetaDataProvider[] GetProviders()
        {
            return this.MetaDataProviderCache.GetProviders(this.GetProvidersCore);
        }

        public IEnumerable<MetaDataProvider> GetProvidersCore()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var set = database.Set<MetaDataProvider>(transaction);
                    //It's easier to just filter enabled/disabled in memory, there isn't much data.
                    //set.Fetch.Filter.AddColumn(
                    //    set.Table.GetColumn(ColumnConfig.By("Enabled", ColumnFlags.None))
                    //).With(filter => filter.Right = filter.CreateConstant(1));
                    foreach (var element in set)
                    {
                        yield return element;
                    }
                }
            }
        }

        public string Checksum
        {
            get
            {
                return "6E3C885D-65D6-4A69-9991-CEC5156121A5";
            }
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            //IMPORTANT: When editing this function remember to change the checksum.
            if (!type.HasFlag(DatabaseInitializeType.MetaData))
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                var set = database.Set<MetaDataProvider>(transaction);
                set.Clear();
                //No default data, yet.
                if (transaction.HasTransaction)
                {
                    transaction.Commit();
                }
            }
        }
    }
}
