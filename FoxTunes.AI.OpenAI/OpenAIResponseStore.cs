#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.Responses;
using OpenAI.VectorStores;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class OpenAIResponseStore : AIResponseStore
    {
        public OpenAIResponseStore(IAIContext context, ResponsesClient client)
        {
            this.Context = context;
            this.Client = client;
        }

        public IAIContext Context { get; }

        public ResponsesClient Client { get; }

        public override async Task<string> Create(string input)
        {
            var options = new CreateResponseOptions()
            {
                Model = this.Context.Model,
                Temperature = this.Context.Temperature
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));
            Logger.Write(this, LogLevel.Debug, "Getting response for prompt: {0}", input);
            var result = await this.Client.CreateResponseAsync(options).ConfigureAwait(false);
            return result.Value.GetOutputText();
        }

        public override async Task<string> Create(string input, string vectorStoreId)
        {
            var options = new CreateResponseOptions()
            {
                Model = this.Context.Model,
                Temperature = this.Context.Temperature
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));
            options.Tools.Add(ResponseTool.CreateFileSearchTool(new[]
            {
                vectorStoreId
            }));
            Logger.Write(this, LogLevel.Debug, "Getting response for prompt: {0}", input);
            Logger.Write(this, LogLevel.Debug, "Using vector store: {0}", vectorStoreId);
            var result = await this.Client.CreateResponseAsync(options).ConfigureAwait(false);
            return result.Value.GetOutputText();
        }
    }
}
