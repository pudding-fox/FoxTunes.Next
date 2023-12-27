using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataSynchronizer : IStandardComponent
    {
        Task Synchronize(params LibraryItem[] libraryItem);

        Task Synchronize(params PlaylistItem[] playlistItem);
    }
}
