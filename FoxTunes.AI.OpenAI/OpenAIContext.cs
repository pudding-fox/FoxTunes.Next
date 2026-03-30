#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI;

namespace FoxTunes
{
    public class OpenAIContext : AIContext
    {
        public OpenAIContext(OpenAIClient client, string model, float temperature, ReasoningLevel reasoningLevel) : base(model, temperature, reasoningLevel)
        {
            this.Client = client;
        }

        public OpenAIClient Client { get; private set; }

        public override IAIFileStore CreateFileStore()
        {
            Logger.Write(this, LogLevel.Debug, "Creating OpenAIFileStore.");
            return new OpenAIFileStore(this, this.Client.GetOpenAIFileClient());
        }

        public override IAIVectorStore CreateVectorStore()
        {
            Logger.Write(this, LogLevel.Debug, "Creating OpenAIVectorStore.");
            return new OpenAIVectorStore(this, this.Client.GetVectorStoreClient());
        }

        public override IAIResponseStore CreateResponseStore()
        {
            Logger.Write(this, LogLevel.Debug, "Creating OpenAIResponseStore.");
            return new OpenAIResponseStore(this, this.Client.GetResponsesClient());
        }
    }
}
