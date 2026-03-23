#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.VectorStores;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class OpenAIVectorStore : AIVectorStore
    {
        public OpenAIVectorStore(IAIContext context, VectorStoreClient client)
        {
            this.Context = context;
            this.Client = client;
        }

        public IAIContext Context { get; private set; }

        public VectorStoreClient Client { get; private set; }

        public override async Task<string> Create(string name)
        {
            var result = await this.Client.CreateVectorStoreAsync().ConfigureAwait(false);
            return result.Value.Id;
        }

        public override async Task AddFile(string vectorStoreId, string fileId)
        {
            var result = await this.Client.AddFileToVectorStoreAsync(vectorStoreId, fileId);
            //Nothing to do.
        }
    }
}
