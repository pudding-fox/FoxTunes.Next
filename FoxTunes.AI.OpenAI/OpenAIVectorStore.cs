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

        public override async Task<string> Create(string name, CancellationToken cancellationToken)
        {
            const int TIMEOUT = 300;
            var id = default(string);
            {
                Logger.Write(this, LogLevel.Debug, "Creating vectore store.");
                var result = await this.Client.CreateVectorStoreAsync().ConfigureAwait(false);
                id = result.Value.Id;
                Logger.Write(this, LogLevel.Debug, "Created vectore store: {0}", id);
            }
            {
                var attempt = 0;
                Logger.Write(this, LogLevel.Debug, "Waiting for vector store to become available.");
            retry:
                await Task.Delay(1000).ConfigureAwait(false);
                var result = await this.Client.GetVectorStoreAsync(id).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreStatus.InProgress)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Warn, "Operation cancelled.");
                        return null;
                    }
                    if (attempt++ < TIMEOUT)
                    {
                        Logger.Write(this, LogLevel.Warn, "Vector store is not yet available, retrying.");
                        goto retry;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Timed out waiting for vector store to become available.");
                        throw new TimeoutException("Timed out waiting for vector store to become available.");
                    }
                }
            }
            return id;
        }

        public override async Task AddFile(string vectorStoreId, string fileId, CancellationToken cancellationToken)
        {
            const int TIMEOUT = 300;
            {
                Logger.Write(this, LogLevel.Debug, "Adding file to vector store.");
                var result = await this.Client.AddFileToVectorStoreAsync(vectorStoreId, fileId).ConfigureAwait(false);
            }
            {
                var attempt = 0;
                Logger.Write(this, LogLevel.Debug, "Waiting for file to become available.");
            retry:
                await Task.Delay(1000).ConfigureAwait(false);
                var result = await this.Client.GetVectorStoreFileAsync(vectorStoreId, fileId).ConfigureAwait(false);
                if (result.Value.Status == VectorStoreFileStatus.InProgress)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Warn, "Operation cancelled.");
                        return;
                    }
                    if (attempt++ < TIMEOUT)
                    {
                        Logger.Write(this, LogLevel.Warn, "File is not yet available, retrying.");
                        goto retry;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Timed out waiting for file to become available.");
                        throw new TimeoutException("Timed out waiting for file to become available.");
                    }
                }
            }
        }

        public override async Task Delete(string vectorStoreId)
        {
            Logger.Write(this, LogLevel.Debug, "Deleting vectore store: {0}", vectorStoreId);
            var result = await this.Client.DeleteVectorStoreAsync(vectorStoreId).ConfigureAwait(false);
            Logger.Write(this, LogLevel.Debug, "Deleted vectore store: {0}", vectorStoreId);
            //Nothing to do.
        }
    }
}
