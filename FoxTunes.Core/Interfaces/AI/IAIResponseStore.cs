using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIResponseStore : IBaseComponent
    {
        Task<string> Create(string input, CancellationToken cancellationToken);

        Task<string> Create(string input, string fileId, string vectorStoreId, CancellationToken cancellationToken);

        event AIResponseStoreReasoningEventHandler Reasoning;
    }

    public delegate void AIResponseStoreReasoningEventHandler(object sender, AIResponseStoreReasoningEventArgs e);

    public class AIResponseStoreReasoningEventArgs : EventArgs
    {
        public AIResponseStoreReasoningEventArgs(string output)
        {
            this.Output = output;
        }

        public string Output { get; private set; }
    }
}
