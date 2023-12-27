using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataBrowser : IStandardComponent
    {
        Task<MetaDataItem[]> GetMetaDatas(LibraryHierarchyNode libraryHierarchyNode, string name, MetaDataItemType type, int limit);

        Task<MetaDataItem[]> GetMetaDatas(IEnumerable<PlaylistItem> playlistItems, string name, MetaDataItemType type, int limit);
    }
}
