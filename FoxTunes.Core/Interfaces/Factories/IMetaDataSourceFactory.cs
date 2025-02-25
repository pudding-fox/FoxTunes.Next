using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataSourceFactory : IStandardFactory
    {
        IEnumerable<KeyValuePair<string, MetaDataItemType>> Supported { get; }

        IMetaDataSource Create();
    }
}
