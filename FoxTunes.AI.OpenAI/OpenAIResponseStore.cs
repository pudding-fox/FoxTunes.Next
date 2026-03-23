#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.Responses;
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

        public override async Task<string> Create(string input, string vectorStoreId)
        {
            var options = new CreateResponseOptions()
            {
                Model = this.Context.Model,
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));
            options.Tools.Add(ResponseTool.CreateFileSearchTool(new[]
            {
                vectorStoreId
            }));
            var result = await this.Client.CreateResponseAsync(options).ConfigureAwait(false);
            return result.Value.GetOutputText();
        }
    }
}
