using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.ReplayGain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassReplayGainScanner : BassTool, IBassReplayGainScanner
    {
        const string GROUP_NONE = "None";

        public BassReplayGainScanner(IEnumerable<ScannerItem> scannerItems)
        {
            this.ScannerItems = scannerItems;
        }

        public IEnumerable<ScannerItem> ScannerItems { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassReplayGainScannerBehaviourConfiguration.THREADS
            ).ConnectValue(value => this.Threads = value);
            base.InitializeComponent(core);
        }

        public void Scan()
        {
            if (this.Threads > 1)
            {
                Logger.Write(this, LogLevel.Debug, "Beginning parallel scanning with {0} threads.", this.Threads);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Beginning single threaded scanning.");
            }
            var scannerItems = default(IEnumerable<ScannerItem>);
            var scannerGroups = default(IEnumerable<IEnumerable<ScannerItem>>);
            this.GetGroups(out scannerItems, out scannerGroups);
            this.ScanTracks(scannerItems);
            this.ScanGroups(scannerGroups);
            Logger.Write(this, LogLevel.Debug, "Scanning completed successfully.");
        }

        protected virtual void ScanTracks(IEnumerable<ScannerItem> scannerItems)
        {
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = this.Threads
            };
            Parallel.ForEach(scannerItems, parallelOptions, scannerItem =>
            {
                try
                {
                    if (this.CancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", scannerItem.FileName);
                        scannerItem.Status = ScannerItemStatus.Cancelled;
                        return;
                    }
                    if (!this.CheckInput(scannerItem.FileName))
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to file \"{1}\" does not exist.", scannerItem.FileName);
                        scannerItem.Status = ScannerItemStatus.Failed;
                        scannerItem.AddError(string.Format("File \"{0}\" does not exist.", scannerItem.FileName));
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Beginning scanning file \"{0}\".", scannerItem.FileName);
                    scannerItem.Progress = ScannerItem.PROGRESS_NONE;
                    scannerItem.Status = ScannerItemStatus.Processing;
                    this.ScanTrack(scannerItem);
                    if (scannerItem.Status == ScannerItemStatus.Complete)
                    {
                        Logger.Write(this, LogLevel.Debug, "Scanning file \"{0}\" completed successfully.", scannerItem.FileName);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Scanning file \"{0}\" failed: Unknown error.", scannerItem.FileName);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", scannerItem.FileName);
                    scannerItem.Status = ScannerItemStatus.Cancelled;
                    return;
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Scanning file \"{0}\" failed: {1}", scannerItem.FileName, e.Message);
                    scannerItem.Status = ScannerItemStatus.Failed;
                    scannerItem.AddError(e.Message);
                }
                finally
                {
                    scannerItem.Progress = ScannerItem.PROGRESS_COMPLETE;
                }
            });
        }

        protected virtual void ScanTrack(ScannerItem scannerItem)
        {
            var success = default(bool);
            var flags = BassFlags.Decode | BassFlags.Float;
            using (var stream = this.CreateStream(scannerItem.FileName, flags))
            {
                if (stream.IsEmpty)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create stream for file \"{0}\": Unknown error.", scannerItem.FileName);
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Created stream for file \"{0}\": {1}", scannerItem.FileName, stream.ChannelHandle);
                using (var monitor = new ChannelMonitor(scannerItem, stream))
                {
                    monitor.Start();
                    success = this.ScanTrack(scannerItem, stream);
                }
            }
            if (this.CancellationToken.IsCancellationRequested)
            {
                scannerItem.Status = ScannerItemStatus.Cancelled;
            }
            else if (success)
            {
                scannerItem.Status = ScannerItemStatus.Complete;
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Scanning file \"{0}\" failed: The format is likely not supported.", scannerItem.FileName);
                scannerItem.AddError("The format is likely not supported.");
                scannerItem.AddError("Only stereo files of common sample rates are supported.");
                scannerItem.Status = ScannerItemStatus.Failed;
            }
        }

        protected virtual bool ScanTrack(ScannerItem scannerItem, IBassStream stream)
        {
            var info = default(ReplayGainInfo);
            if (!BassReplayGain.Process(stream.ChannelHandle, out info))
            {
                return false;
            }
            scannerItem.ItemPeak = info.peak;
            scannerItem.ItemGain = info.gain;
            return true;
        }

        protected virtual void ScanGroups(IEnumerable<IEnumerable<ScannerItem>> groups)
        {
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = this.Threads
            };
            Parallel.ForEach(groups, parallelOptions, group =>
            {
                var streams = new Dictionary<ScannerItem, IBassStream>();
                foreach (var scannerItem in group)
                {
                    try
                    {
                        if (this.CancellationToken.IsCancellationRequested)
                        {
                            Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", scannerItem.FileName);
                            scannerItem.Status = ScannerItemStatus.Cancelled;
                            continue;
                        }
                        if (!this.CheckInput(scannerItem.FileName))
                        {
                            Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to file \"{1}\" does not exist.", scannerItem.FileName);
                            scannerItem.Status = ScannerItemStatus.Failed;
                            scannerItem.AddError(string.Format("File \"{0}\" does not exist.", scannerItem.FileName));
                            continue;
                        }
                        var flags = BassFlags.Decode | BassFlags.Float;
                        var stream = this.CreateStream(scannerItem.FileName, flags);
                        if (stream.IsEmpty)
                        {
                            Logger.Write(this, LogLevel.Warn, "Failed to create stream for file \"{0}\": Unknown error.", scannerItem.FileName);
                            continue;
                        }
                        Logger.Write(this, LogLevel.Debug, "Created stream for file \"{0}\": {1}", scannerItem.FileName, stream.ChannelHandle);
                        streams.Add(scannerItem, stream);
                        Logger.Write(this, LogLevel.Debug, "Beginning scanning file \"{0}\".", scannerItem.FileName);
                        scannerItem.Progress = ScannerItem.PROGRESS_NONE;
                        scannerItem.Status = ScannerItemStatus.Processing;
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", scannerItem.FileName);
                        scannerItem.Status = ScannerItemStatus.Cancelled;
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Scanning file \"{0}\" failed: {1}", scannerItem.FileName, e.Message);
                        scannerItem.Status = ScannerItemStatus.Failed;
                        scannerItem.AddError(e.Message);
                    }
                }
                var success = default(bool);
                var monitors = new List<ChannelMonitor>();
                try
                {
                    foreach (var pair in streams)
                    {
                        var monitor = new ChannelMonitor(pair.Key, pair.Value);
                        monitor.Start();
                        monitors.Add(monitor);
                    }
                    success = this.ScanGroup(streams);
                }
                finally
                {
                    foreach (var monitor in monitors)
                    {
                        monitor.Dispose();
                    }
                    foreach (var stream in streams.Values)
                    {
                        stream.Dispose();
                    }
                }
                foreach (var scannerItem in streams.Keys)
                {
                    if (this.CancellationToken.IsCancellationRequested)
                    {
                        scannerItem.Status = ScannerItemStatus.Cancelled;
                    }
                    else if (success)
                    {
                        Logger.Write(this, LogLevel.Debug, "Scanning file \"{0}\" completed successfully.", scannerItem.FileName);
                        scannerItem.Status = ScannerItemStatus.Complete;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Scanning file \"{0}\" failed: The format is likely not supported.", scannerItem.FileName);
                        scannerItem.AddError("The format is likely not supported.");
                        scannerItem.Status = ScannerItemStatus.Failed;
                    }
                }
            });
        }

        protected virtual bool ScanGroup(IDictionary<ScannerItem, IBassStream> group)
        {
            if (group.Count == 0)
            {
                //Nothing to do.
                return true;
            }
            var channelHandles = group.Values
                .Select(stream => stream.ChannelHandle)
                .ToArray();
            var info = default(ReplayGainBatchInfo);
            if (!BassReplayGain.ProcessBatch(channelHandles, out info))
            {
                return false;
            }
            foreach (var pair in group)
            {
                foreach (var item in info.items)
                {
                    if (item.handle == pair.Value.ChannelHandle)
                    {
                        pair.Key.ItemPeak = item.peak;
                        pair.Key.ItemGain = item.gain;
                        pair.Key.GroupPeak = info.peak;
                        pair.Key.GroupGain = info.gain;
                        break;
                    }
                }
            }
            return true;
        }

        protected virtual void GetGroups(out IEnumerable<ScannerItem> scannerItems, out IEnumerable<IEnumerable<ScannerItem>> scannerGroups)
        {
            var items = new List<ScannerItem>();
            var groups = new Dictionary<string, List<ScannerItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var scannerItem in this.ScannerItems)
            {
                if (scannerItem.Mode != ReplayGainMode.Album)
                {
                    items.Add(scannerItem);
                    continue;
                }
                var name = scannerItem.GroupName;
                if (string.IsNullOrEmpty(name))
                {
                    name = GROUP_NONE;
                }
                var group = groups.GetOrAdd(name, key => new List<ScannerItem>());
                group.Add(scannerItem);
            }
            scannerItems = items;
            scannerGroups = groups.Values;
        }
    }
}
