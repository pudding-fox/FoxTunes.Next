using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ReportEmitter : StandardComponent, IReportEmitter
    {
        public Task Send(IReportComponent report)
        {
            return this.OnReport(report);
        }

        protected virtual Task OnReport(IReportComponent report)
        {
            if (this.Report == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Report(this, report);
        }

        public event ReportEventHandler Report;
    }
}
