using LLama;
using LLama.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LLamaAIContext : AIContext
    {
        public LLamaAIContext(ChatSession session, InferenceParams inferenceParams)
        {
            this.Session = session;
            this.InferenceParams = inferenceParams;
        }

        public ChatSession Session { get; private set; }

        public InferenceParams InferenceParams { get; private set; }

        public override async Task<IEnumerable<string>> Chat(string prompt)
        {
            var history = new ChatHistory.Message(AuthorRole.User, prompt);
            var result = new List<string>();
            await foreach (var response in this.Session.ChatAsync(history, this.InferenceParams).ConfigureAwait(false))
            {
                result.Add(response);
            }
            return result;
        }
    }
}
