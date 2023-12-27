using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IReportEmitter : IReportSource
    {
        Task Send(IReportComponent report);
    }
}
