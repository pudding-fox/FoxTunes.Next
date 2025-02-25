using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataManager : IStandardManager
    {
        Task Rescan(IEnumerable<IFileData> fileDatas, MetaDataUpdateFlags flags);

        Task Save(IEnumerable<IFileData> fileDatas, IEnumerable<string> names, MetaDataUpdateType updateType, MetaDataUpdateFlags flags);

        Task Synchronize();

        Task<bool> Synchronize(IEnumerable<IFileData> fileDatas, params string[] names);
    }
}
