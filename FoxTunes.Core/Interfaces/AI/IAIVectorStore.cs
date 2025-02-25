using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIVectorStore
    {
        Task<string> Create(string name, CancellationToken cancellationToken);

        Task AddFile(string vectorStoreId, string fileId, CancellationToken cancellationToken);

        Task Delete(string vectorStoreId);
    }
}
