using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOnDemandMetaDataProvider : IStandardComponent
    {
        bool IsSourceEnabled(string name, MetaDataItemType type);

        Task<string> GetMetaData(IFileData fileData, OnDemandMetaDataRequest request);

        Task<IEnumerable<string>> GetMetaData(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request);

        string GetCurrentMetaData(IFileData fileData, OnDemandMetaDataRequest request);

        IDictionary<IFileData, string> GetCurrentMetaData(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request);

        Task SetMetaData(OnDemandMetaDataRequest request, OnDemandMetaDataValues result);
    }
}
