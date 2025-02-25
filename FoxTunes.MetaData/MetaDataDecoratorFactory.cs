using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class MetaDataDecoratorFactory : StandardFactory, IMetaDataDecoratorFactory
    {
        public ICore Core { get; private set; }

        public IMetaDataProviderManager MetaDataProviderManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.MetaDataProviderManager = core.Managers.MetaDataProvider;
            base.InitializeComponent(core);
        }

        public IEnumerable<KeyValuePair<string, MetaDataItemType>> Supported
        {
            get
            {
                var providers = this.MetaDataProviderManager.GetProviders();
                foreach (var provider in providers)
                {
                    if (!provider.Enabled)
                    {
                        continue;
                    }
                    yield return new KeyValuePair<string, MetaDataItemType>(provider.Name, MetaDataItemType.CustomTag);
                }
            }
        }

        public bool CanCreate
        {
            get
            {
                var providers = this.MetaDataProviderManager.GetProviders();
                return providers.Any(provider => provider.Enabled);
            }
        }

        public IMetaDataDecorator Create()
        {
            var decorator = new MetaDataDecorator();
            decorator.InitializeComponent(this.Core);
            return decorator;
        }
    }
}
