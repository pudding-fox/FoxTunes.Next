#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.Responses;
using System.ClientModel;
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

        public override async Task<string> Create(string input, CancellationToken cancellationToken)
        {
            var options = new CreateResponseOptions()
            {
                Model = this.Context.Model,
                Temperature = this.Context.Temperature
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));
            Logger.Write(this, LogLevel.Debug, "Getting response for prompt: {0}", input);
        retry:
            try
            {
                var result = await this.Client.CreateResponseAsync(options, cancellationToken.ToNative()).ConfigureAwait(false);
                return result.Value.GetOutputText();
            }
            catch (ClientResultException e)
            {
                if (e.Message.Contains("temperature", true))
                {
                    options.Temperature = null;
                    goto retry;
                }
                throw;
            }
        }

        public override async Task<string> Create(string input, string fileId, string vectorStoreId, CancellationToken cancellationToken)
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
        retry:
            try
            {
                var result = await this.Client.CreateResponseAsync(options, cancellationToken.ToNative()).ConfigureAwait(false);
                return result.Value.GetOutputText();
            }
            catch (ClientResultException e)
            {
                if (e.Message.Contains("temperature", true))
                {
                    options.Temperature = null;
                    goto retry;
                }
                throw;
            }
        }
    }
}
