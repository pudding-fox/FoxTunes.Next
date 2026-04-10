#pragma warning disable OPENAI001
using FoxTunes.Interfaces;
using OpenAI.Responses;
using System.ClientModel;
using System.Text;
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
                Temperature = this.Context.Temperature,
                ReasoningOptions = new ResponseReasoningOptions()
                {
                    ReasoningEffortLevel = GetReasoningEffortLevel(this.Context.ReasoningLevel)
                },
                StreamingEnabled = true
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));
            Logger.Write(this, LogLevel.Debug, "Getting response for prompt: {0}", input);
        retry:
            try
            {
                return await this.GetResponseText(this.Client.CreateResponseStreamingAsync(options, cancellationToken.ToNative()));
            }
            catch (ClientResultException e)
            {
                if (e.Message.Contains("temperature", true))
                {
                    options.Temperature = null;
                    goto retry;
                }
                else if (e.Message.Contains("reasoning", true))
                {
                    options.ReasoningOptions = null;
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
                Temperature = this.Context.Temperature,
                ReasoningOptions = new ResponseReasoningOptions()
                {
                    ReasoningEffortLevel = GetReasoningEffortLevel(this.Context.ReasoningLevel)
                },
                StreamingEnabled = true
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
                return await this.GetResponseText(this.Client.CreateResponseStreamingAsync(options, cancellationToken.ToNative()));
            }
            catch (ClientResultException e)
            {
                if (e.Message.Contains("temperature", true))
                {
                    options.Temperature = null;
                    goto retry;
                }
                else if (e.Message.Contains("reasoning", true))
                {
                    options.ReasoningOptions = null;
                    goto retry;
                }
                throw;
            }
        }

        private ResponseReasoningEffortLevel GetReasoningEffortLevel(ReasoningLevel reasoningLevel)
        {
            switch (reasoningLevel)
            {
                case ReasoningLevel.None:
                    return ResponseReasoningEffortLevel.None;
                case ReasoningLevel.Minimal:
                    return ResponseReasoningEffortLevel.Minimal;
                case ReasoningLevel.Low:
                    return ResponseReasoningEffortLevel.Low;
                default:
                case ReasoningLevel.Medium:
                    return ResponseReasoningEffortLevel.Medium;
                case ReasoningLevel.High:
                    return ResponseReasoningEffortLevel.High;
            }
        }

        protected virtual async Task<string> GetResponseText(AsyncCollectionResult<StreamingResponseUpdate> result)
        {
            var builder = new StringBuilder();
            await foreach (var update in result)
            {
                switch (update)
                {
                    case StreamingResponseReasoningTextDeltaUpdate reasoning:
                        this.OnReasoning(reasoning.Delta);
                        break;
                    case StreamingResponseOutputTextDeltaUpdate output:
                        builder.Append(output.Delta);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
