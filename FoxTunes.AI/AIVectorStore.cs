using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIVectorStore : StandardComponent, IAIVectorStore
    {
        public abstract Task<string> Create(string name, CancellationToken cancellationToken);

        public abstract Task AddFile(string vectorStoreId, string fileId, CancellationToken cancellationToken);

        public abstract Task Delete(string vectorStoreId);
    }
}
