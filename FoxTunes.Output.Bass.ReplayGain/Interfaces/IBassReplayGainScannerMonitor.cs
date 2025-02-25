using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public interface IBassReplayGainScannerMonitor : IReportsProgress
    {
        Task Scan();
    }
}
