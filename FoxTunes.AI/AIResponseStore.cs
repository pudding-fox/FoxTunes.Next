using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIResponseStore : BaseComponent, IAIResponseStore
    {
        public abstract Task<string> Create(string input, CancellationToken cancellationToken);

        public abstract Task<string> Create(string input, string fileId, string vectorStoreId, CancellationToken cancellationToken);

        protected virtual void OnReasoning(string output)
        {
            if (this.Reasoning == null)
            {
                return;
            }
            this.Reasoning(this, new AIResponseStoreReasoningEventArgs(output));
        }

        public event AIResponseStoreReasoningEventHandler Reasoning;
    }
}
