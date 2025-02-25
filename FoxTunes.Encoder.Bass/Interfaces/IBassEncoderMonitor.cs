using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public interface IBassEncoderMonitor : IReportsProgress
    {
        Task Encode();
    }
}
