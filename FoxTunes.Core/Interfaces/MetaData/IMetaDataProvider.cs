using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataProvider : IBaseComponent
    {
        MetaDataProviderType Type { get; }

        bool AddOrUpdate(string fileName, IList<MetaDataItem> metaDataItems, MetaDataProvider provider);

        bool AddOrUpdate(IFileAbstraction fileAbstraction, IList<MetaDataItem> metaDataItems, MetaDataProvider provider);
    }
}
