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
            var id = default(string);
            {
                var result = await this.Client.CreateVectorStoreAsync().ConfigureAwait(false);
                id = result.Value.Id;
            }
            {
            retry:
                var result = await this.Client.GetVectorStoreAsync(id).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreStatus.InProgress)
                {
                    goto retry;
                }
            }
            return id;
        }

        public override async Task AddFile(string vectorStoreId, string fileId)
        {
            {
                var result = await this.Client.AddFileToVectorStoreAsync(vectorStoreId, fileId).ConfigureAwait(false);
            }
            {
            retry:
                var result = await this.Client.GetVectorStoreFileAsync(vectorStoreId, fileId).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreFileStatus.InProgress)
                {
                    goto retry;
                }
            }
        }

        public override async Task Delete(string vectorStoreId)
        {
            var result = await this.Client.DeleteVectorStoreAsync(vectorStoreId).ConfigureAwait(false);
            //Nothing to do.
        }
    }
}
