using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIResponseStore : BaseComponent, IAIResponseStore
    {
        public abstract Task<string> Create(string input);

        public abstract Task<string> Create(string input, string vectorStoreId);
    }
}
