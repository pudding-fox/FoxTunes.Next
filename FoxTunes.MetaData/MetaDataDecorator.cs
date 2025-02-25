using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class MetaDataDecorator : BaseComponent, IMetaDataDecorator
    {
        public MetaDataDecorator()
        {
            this.Warnings = new ConcurrentDictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public ConcurrentDictionary<string, IList<string>> Warnings { get; private set; }

        public IEnumerable<string> GetWarnings(string fileName)
        {
            var warnings = default(IList<string>);
            if (!this.Warnings.TryGetValue(fileName, out warnings))
            {
                return Enumerable.Empty<string>();
            }
            return warnings;
        }

        public void AddWarning(string fileName, string warning)
        {
            this.Warnings.GetOrAdd(fileName, key => new List<string>()).Add(warning);
        }

        public IMetaDataProviderManager MetaDataProviderManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataProviderManager = core.Managers.MetaDataProvider;
            base.InitializeComponent(core);
        }

        public void Decorate(string fileName, IList<MetaDataItem> metaDataItems, ISet<string> names = null)
        {
            var providers = this.MetaDataProviderManager.GetProviders();
            foreach (var provider in providers)
            {
                if (!provider.Enabled)
                {
                    continue;
                }
                try
                {
                    var factory = this.MetaDataProviderManager.GetProvider(provider);
                    if (factory == null)
                    {
                        //No such IMetaDataProvider implementation for MetaDataProviderType.
                        continue;
                    }
                    if (factory.AddOrUpdate(fileName, metaDataItems, provider) && names != null)
                    {
                        names.Add(provider.Name);
                    }
                }
                catch (Exception e)
                {
                    this.AddWarning(fileName, e.Message);
                }
            }
        }

        public void Decorate(IFileAbstraction fileAbstraction, IList<MetaDataItem> metaDataItems, ISet<string> names = null)
        {
            var providers = this.MetaDataProviderManager.GetProviders();
            foreach (var provider in providers)
            {
                if (!provider.Enabled)
                {
                    continue;
                }
                try
                {
                    var factory = this.MetaDataProviderManager.GetProvider(provider);
                    if (factory == null)
                    {
                        //No such IMetaDataProvider implementation for MetaDataProviderType.
                        continue;
                    }
                    if (factory.AddOrUpdate(fileAbstraction, metaDataItems, provider) && names != null)
                    {
                        names.Add(provider.Name);
                    }
                }
                catch (Exception e)
                {
                    this.AddWarning(fileAbstraction.FileName, e.Message);
                }
            }
        }
    }
}
