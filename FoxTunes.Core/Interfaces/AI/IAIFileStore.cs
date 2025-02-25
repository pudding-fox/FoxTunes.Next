using System.IO;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIFileStore : IBaseComponent
    {
        Task<string> Create(Stream content, string fileName, CancellationToken cancellationToken);

        Task Delete(string fileId);
    }
}
