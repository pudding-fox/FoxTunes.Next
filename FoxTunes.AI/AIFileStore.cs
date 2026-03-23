using FoxTunes.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIFileStore : BaseComponent, IAIFileStore
    {
        public abstract Task<string> Create(Stream content, string fileName);

        public abstract Task Delete(string fileId);
    }
}
