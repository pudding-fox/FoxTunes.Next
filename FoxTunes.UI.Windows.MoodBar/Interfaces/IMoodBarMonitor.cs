using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public interface IMoodBarMonitor : IReportsProgress
    {
        Task Create();
    }
}
