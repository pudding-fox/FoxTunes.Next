using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassReplayGainScannerMonitor : PopulatorBase, IBassReplayGainScannerMonitor
    {
        public static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);

        public BassReplayGainScannerMonitor(IBassReplayGainScanner scanner, bool reportProgress, CancellationToken cancellationToken) : base(reportProgress)
        {
            this.ScannerItems = new Dictionary<Guid, ScannerItem>();
            this.Scanner = scanner;
            this.CancellationToken = cancellationToken;
        }

        public Dictionary<Guid, ScannerItem> ScannerItems { get; private set; }

        public IBassReplayGainScanner Scanner { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task Scan()
        {
#if NET40
            var task = TaskEx.Run(() =>
#else
            var task = Task.Run(() =>
#endif
            {
                this.Scanner.Scan();
            });
#if NET40
            return TaskEx.WhenAll(task, this.Monitor(task));
#else
            return Task.WhenAll(task, this.Monitor(task));
#endif
        }

        protected virtual async Task Monitor(Task task)
        {
            this.Name = "Scanning files";
            while (!task.IsCompleted)
            {
                if (this.CancellationToken.IsCancellationRequested)
                {
                    Logger.Write(this, LogLevel.Debug, "Requesting cancellation from scanner.");
                    this.Scanner.Cancel();
                    this.Name = "Cancelling";
                    break;
                }
                this.Scanner.Update();
                var position = 0;
                var count = 0;
                var builder = new StringBuilder();
                foreach (var scannerItem in this.Scanner.ScannerItems)
                {
                    this.ScannerItems[scannerItem.Id] = scannerItem;
                    position += scannerItem.Progress;
                    count += ScannerItem.PROGRESS_COMPLETE;
                    if (scannerItem.Status == ScannerItemStatus.Processing && scannerItem.Progress != ScannerItem.PROGRESS_COMPLETE)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append(", ");
                        }
                        builder.Append(Path.GetFileName(scannerItem.FileName));
                    }
                }
                if (builder.Length > 0)
                {
                    this.Description = builder.ToString();
                }
                else
                {
                    this.Description = "Waiting for scanner";
                }
                this.Position = position;
                this.Count = count;
#if NET40
                await TaskEx.Delay(INTERVAL).ConfigureAwait(false);
#else
                await Task.Delay(INTERVAL).ConfigureAwait(false);
#endif
            }
            while (!task.IsCompleted)
            {
                Logger.Write(this, LogLevel.Debug, "Waiting for scanner to complete.");
                this.Scanner.Update();
#if NET40
                await TaskEx.Delay(INTERVAL).ConfigureAwait(false);
#else
                await Task.Delay(INTERVAL).ConfigureAwait(false);
#endif
            }
        }
    }

    public delegate void BassScannerMonitorEventHandler(object sender, BassScannerMonitorEventArgs e);

    public class BassScannerMonitorEventArgs : EventArgs
    {
        public BassScannerMonitorEventArgs(ScannerItem scannerItem)
        {
            this.ScannerItem = scannerItem;
        }

        public ScannerItem ScannerItem { get; private set; }
    }
}
