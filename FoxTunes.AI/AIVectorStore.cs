using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIVectorStore : StandardComponent, IAIVectorStore
    {
        public abstract Task<string> Create(string name);

        public abstract Task AddFile(string vectorStoreId, string fileId);
    }
}
