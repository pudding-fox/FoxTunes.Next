#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI;

namespace FoxTunes
{
    public class OpenAIContext : AIContext
    {
        public OpenAIContext(OpenAIClient client, string model) : base(model)
        {
            this.Client = client;
        }

        public OpenAIClient Client { get; private set; }

        public override IAIFileStore CreateFileStore()
        {
            return new OpenAIFileStore(this, this.Client.GetOpenAIFileClient());
        }

        public override IAIVectorStore CreateVectorStore()
        {
            return new OpenAIVectorStore(this, this.Client.GetVectorStoreClient());
        }

        public override IAIResponseStore CreateResponseStore()
        {
            return new OpenAIResponseStore(this, this.Client.GetResponsesClient());
        }
    }
}
