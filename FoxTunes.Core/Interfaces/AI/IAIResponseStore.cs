using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIResponseStore : IBaseComponent
    {
        Task<string> Create(string input, CancellationToken cancellationToken);

        Task<string> Create(string input, string fileId, string vectorStoreId, CancellationToken cancellationToken);
    }
}
