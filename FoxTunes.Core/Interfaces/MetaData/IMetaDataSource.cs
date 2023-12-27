using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataSource : IBaseComponent, IInitializable
    {
        IEnumerable<string> GetWarnings(string fileName);

        Task<IEnumerable<MetaDataItem>> GetMetaData(string fileName);

        Task<IEnumerable<MetaDataItem>> GetMetaData(IFileAbstraction fileAbstraction);

        Task SetMetaData(string fileName, IEnumerable<MetaDataItem> metaDataItems, Func<MetaDataItem, bool> predicate);
    }

    [Flags]
    public enum ArtworkType : byte
    {
        None = 0,
        FrontCover = 1,
        BackCover = 2,
        Unknown = 255
    }
}
