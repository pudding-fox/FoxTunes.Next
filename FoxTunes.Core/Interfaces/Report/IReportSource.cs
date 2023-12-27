using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IReportSource : IStandardComponent
    {
        event ReportEventHandler Report;
    }

    public delegate Task ReportEventHandler(object sender, IReportComponent report);
}
