using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassCdMetaDataSource : BaseComponent, IMetaDataSource
    {
        public BassCdMetaDataSource(IBassCdMetaDataSourceStrategy strategy)
        {
            this.Strategy = strategy;
        }

        public IBassCdMetaDataSourceStrategy Strategy { get; private set; }

        public IEnumerable<string> GetWarnings(string fileName)
        {
            return Enumerable.Empty<string>();
        }

        public Task<IEnumerable<MetaDataItem>> GetMetaData(string fileName)
        {
            var drive = default(int);
            var id = default(string);
            var track = default(int);
            if (!CdUtils.ParseUrl(fileName, out drive, out id, out track))
            {
                //TODO: Warn.
#if NET40
                return TaskEx.FromResult(Enumerable.Empty<MetaDataItem>());
#else
                return Task.FromResult(Enumerable.Empty<MetaDataItem>());
#endif
            }
            var metaData = new List<MetaDataItem>();
            metaData.AddRange(this.Strategy.GetMetaDatas(track));
            metaData.AddRange(this.Strategy.GetProperties(track));
#if NET40
            return TaskEx.FromResult<IEnumerable<MetaDataItem>>(metaData);
#else
            return Task.FromResult<IEnumerable<MetaDataItem>>(metaData);
#endif
        }

        public Task<IEnumerable<MetaDataItem>> GetMetaData(IFileAbstraction fileAbstraction)
        {
            throw new NotImplementedException();
        }

        public Task SetMetaData(string fileName, IEnumerable<MetaDataItem> metaDataItems, Func<MetaDataItem, bool> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
