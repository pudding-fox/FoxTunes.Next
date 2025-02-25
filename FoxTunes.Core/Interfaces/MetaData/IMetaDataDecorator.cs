using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataDecorator : IBaseComponent
    {
        IEnumerable<string> GetWarnings(string fileName);

        void Decorate(string fileName, IList<MetaDataItem> metaDataItems, ISet<string> names = null);

        void Decorate(IFileAbstraction fileAbstraction, IList<MetaDataItem> metaDataItems, ISet<string> names = null);
    }
}
