using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassEncoderMonitor : PopulatorBase, IBassEncoderMonitor
    {
        public static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);

        public BassEncoderMonitor(IBassEncoder encoder, bool reportProgress, CancellationToken cancellationToken) : base(reportProgress)
        {
            this.EncoderItems = new Dictionary<Guid, EncoderItem>();
            this.Encoder = encoder;
            this.CancellationToken = cancellationToken;
        }

        public Dictionary<Guid, EncoderItem> EncoderItems { get; private set; }

        public IBassEncoder Encoder { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task Encode()
        {
#if NET40
            var task = TaskEx.Run(() =>
#else
            var task = Task.Run(() =>
#endif
            {
                this.Encoder.Encode();
            });
#if NET40
            return TaskEx.WhenAll(task, this.Monitor(task));
#else
            return Task.WhenAll(task, this.Monitor(task));
#endif
        }

        protected virtual async Task Monitor(Task task)
        {
            this.Name = "Converting files";
            while (!task.IsCompleted)
            {
                if (this.CancellationToken.IsCancellationRequested)
                {
                    Logger.Write(this, LogLevel.Debug, "Requesting cancellation from encoder.");
                    this.Encoder.Cancel();
                    this.Name = "Cancelling";
                    break;
                }
                this.Encoder.Update();
                var position = 0;
                var count = 0;
                var builder = new StringBuilder();
                foreach (var encoderItem in this.Encoder.EncoderItems)
                {
                    this.UpdateStatus(encoderItem);
                    position += encoderItem.Progress;
                    count += EncoderItem.PROGRESS_COMPLETE;
                    if (encoderItem.Status == EncoderItemStatus.Processing && encoderItem.Progress != EncoderItem.PROGRESS_COMPLETE)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append(", ");
                        }
                        builder.Append(Path.GetFileName(encoderItem.OutputFileName));
                    }
                }
                if (builder.Length > 0)
                {
                    this.Description = builder.ToString();
                }
                else
                {
                    this.Description = "Waiting for encoder";
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
                Logger.Write(this, LogLevel.Debug, "Waiting for encoder to complete.");
                this.Encoder.Update();
#if NET40
                await TaskEx.Delay(INTERVAL).ConfigureAwait(false);
#else
                await Task.Delay(INTERVAL).ConfigureAwait(false);
#endif
            }
        }

        protected virtual void UpdateStatus(EncoderItem encoderItem)
        {
            var currentEncoderItem = default(EncoderItem);
            if (this.EncoderItems.TryGetValue(encoderItem.Id, out currentEncoderItem) && currentEncoderItem.Status != encoderItem.Status)
            {
                Logger.Write(this, LogLevel.Debug, "Encoder status changed for file \"{0}\": {1}", encoderItem.InputFileName, Enum.GetName(typeof(EncoderItemStatus), encoderItem.Status));
                this.OnStatusChanged(encoderItem);
            }
            this.EncoderItems[encoderItem.Id] = encoderItem;
        }

        protected virtual void OnStatusChanged(EncoderItem encoderItem)
        {
            if (this.StatusChanged == null)
            {
                return;
            }
            this.StatusChanged(this, new BassEncoderMonitorEventArgs(encoderItem));
        }

        public event BassEncoderMonitorEventHandler StatusChanged;
    }

    public delegate void BassEncoderMonitorEventHandler(object sender, BassEncoderMonitorEventArgs e);

    public class BassEncoderMonitorEventArgs : EventArgs
    {
        public BassEncoderMonitorEventArgs(EncoderItem encoderItem)
        {
            this.EncoderItem = encoderItem;
        }

        public EncoderItem EncoderItem { get; private set; }
    }
}
