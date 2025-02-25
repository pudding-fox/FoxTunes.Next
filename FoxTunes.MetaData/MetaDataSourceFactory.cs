using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class MetaDataSourceFactory : StandardFactory, IMetaDataSourceFactory
    {
        public ICore Core { get; private set; }

        public IMetaDataDecoratorFactory MetaDataDecoratorFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public abstract IEnumerable<KeyValuePair<string, MetaDataItemType>> Supported { get; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.MetaDataDecoratorFactory = core.Factories.MetaDataDecorator;
            this.Configuration = core.Components.Configuration;
            base.InitializeComponent(core);
        }

        public IMetaDataSource Create()
        {
            var metaDataSource = this.OnCreate();
            if (this.MetaDataDecoratorFactory.CanCreate)
            {
                var metaDataDecorator = this.MetaDataDecoratorFactory.Create();
                metaDataSource = new MetaDataSourceWrapper(metaDataSource, metaDataDecorator);
                metaDataSource.InitializeComponent(this.Core);
            }
            return metaDataSource;
        }

        public abstract IMetaDataSource OnCreate();
    }
}
