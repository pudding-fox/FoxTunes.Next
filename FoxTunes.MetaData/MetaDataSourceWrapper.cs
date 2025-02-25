using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MetaDataSourceWrapper : BaseComponent, IMetaDataSource
    {
        public MetaDataSourceWrapper(IMetaDataSource metaDataSource, IMetaDataDecorator metaDataDecorator) 
        {
            this.MetaDataSource = metaDataSource;
            this.MetaDataDecorator = metaDataDecorator;
        }

        public IMetaDataSource MetaDataSource { get; private set; }

        public IMetaDataDecorator MetaDataDecorator { get; private set; }

        public async Task<IEnumerable<MetaDataItem>> GetMetaData(string fileName)
        {
            var metaDataItems = (await this.MetaDataSource.GetMetaData(fileName).ConfigureAwait(false)).AsList();
            this.MetaDataDecorator.Decorate(fileName, metaDataItems);
            return metaDataItems;
        }

        public async Task<IEnumerable<MetaDataItem>> GetMetaData(IFileAbstraction fileAbstraction)
        {
            var metaDataItems = (await this.MetaDataSource.GetMetaData(fileAbstraction).ConfigureAwait(false)).AsList();
            this.MetaDataDecorator.Decorate(fileAbstraction, metaDataItems);
            return metaDataItems;
        }

        public IEnumerable<string> GetWarnings(string fileName)
        {
            return this.MetaDataSource.GetWarnings(fileName)
                .Concat(this.MetaDataDecorator.GetWarnings(fileName))
                .ToArray();
        }

        public Task SetMetaData(string fileName, IEnumerable<MetaDataItem> metaDataItems, Func<MetaDataItem, bool> predicate)
        {
            return this.MetaDataSource.SetMetaData(fileName, metaDataItems, predicate);
        }
    }
}
