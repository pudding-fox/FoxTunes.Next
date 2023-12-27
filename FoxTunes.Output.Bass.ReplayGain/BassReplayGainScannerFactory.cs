using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassReplayGainScannerFactory : StandardComponent, IBassReplayGainScannerFactory
    {
        public string Location
        {
            get
            {
                return typeof(BassReplayGainScannerFactory).Assembly.Location;
            }
        }

        public IBassReplayGainScanner CreateScanner(IEnumerable<ScannerItem> scannerItems)
        {
            var process = this.CreateProcess();
            var proxy = new BassScannerProxy(process, scannerItems);
            return proxy;
        }

        protected virtual Process CreateProcess()
        {
            Logger.Write(this, LogLevel.Debug, "Creating scanner container process: {0}", this.Location);
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = this.Location,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(processStartInfo);
            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    return;
                }
                Logger.Write(this, LogLevel.Trace, "{0}: {1}", this.Location, e.Data);
            };
            process.BeginErrorReadLine();
            return process;
        }
    }
}
