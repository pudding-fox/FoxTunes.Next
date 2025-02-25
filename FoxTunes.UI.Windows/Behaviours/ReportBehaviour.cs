using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ReportBehaviour : StandardBehaviour, IDisposable
    {
        public IReportEmitter ReportEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.ReportEmitter = core.Components.ReportEmitter;
            this.ReportEmitter.Report += this.OnReport;
            base.InitializeComponent(core);
        }

        protected virtual Task OnReport(object sender, IReportComponent report)
        {
            return Windows.Invoke(() =>
            {
                var window = new ReportWindow()
                {
                    Source = report,
                    ShowActivated = true,
                    Owner = Windows.ActiveWindow
                };
                if (report.IsDialog)
                {
                    window.ShowDialog();
                }
                else
                {
                    window.Show();
                }
            });
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.ReportEmitter != null)
            {
                this.ReportEmitter.Report -= this.OnReport;
            }
        }

        ~ReportBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
