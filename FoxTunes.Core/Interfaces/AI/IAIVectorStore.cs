using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIVectorStore
    {
        Task<string> Create(string name);

        Task AddFile(string vectorStoreId, string fileId);

        Task Delete(string vectorStoreId);
    }
}
