using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IAIResponseStore : IBaseComponent
    {
        Task<string> Create(string input, string vectorStoreId);
    }
}
