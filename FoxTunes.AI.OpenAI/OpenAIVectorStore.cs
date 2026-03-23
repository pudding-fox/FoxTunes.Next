#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.VectorStores;
using System;
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
                var attempt = 0;
            retry:
                var result = await this.Client.GetVectorStoreAsync(id).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreStatus.InProgress)
                {
                    if (attempt++ < 5)
                    {
                        goto retry;
                    }
                    else
                    {
                        throw new TimeoutException("Timed out waiting for vector store to become available.");
                    }
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
                var attempt = 0;
            retry:
                var result = await this.Client.GetVectorStoreFileAsync(vectorStoreId, fileId).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreFileStatus.InProgress)
                {
                    if (attempt++ < 3)
                    {
                        goto retry;
                    }
                    else
                    {
                        throw new TimeoutException("Timed out waiting for file to become available.");
                    }
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
