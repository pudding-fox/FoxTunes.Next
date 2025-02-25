using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataDecoratorFactory : IStandardFactory
    {
        IEnumerable<KeyValuePair<string, MetaDataItemType>> Supported { get; }

        bool CanCreate { get; }

        IMetaDataDecorator Create();
    }
}
