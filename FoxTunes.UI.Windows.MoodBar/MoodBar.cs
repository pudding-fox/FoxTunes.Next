using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MoodBar : BassTool, IMoodBar
    {
        public MoodBar(IEnumerable<MoodBarItem> MoodBarItems)
        {
            this.MoodBarItems = MoodBarItems.ToList();
        }

        public IList<MoodBarItem> MoodBarItems { get; private set; }

        IEnumerable<MoodBarItem> IMoodBar.MoodBarItems
        {
            get
            {
                return this.MoodBarItems;
            }
        }

        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public async Task Create()
        {
            if (this.Threads > 1)
            {
                Logger.Write(this, LogLevel.Debug, "Beginning parallel creation with {0} threads.", this.Threads);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Beginning single threaded creation.");
            }
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Math.Max(this.Threads, 1)
            };
            await AsyncParallel.ForEach(this.MoodBarItems.ToArray(), async moodBarItem =>
            {
                try
                {
                    if (this.CancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", moodBarItem.FileName);
                        moodBarItem.Status = MoodBarItemStatus.Cancelled;
                        return;
                    }
                    if (!this.CheckInput(moodBarItem.FileName))
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to file \"{1}\" does not exist.", moodBarItem.FileName);
                        moodBarItem.Status = MoodBarItemStatus.Failed;
                        moodBarItem.AddError(string.Format("File \"{0}\" does not exist.", moodBarItem.FileName));
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Beginning creating file \"{0}\".", moodBarItem.FileName);
                    moodBarItem.Progress = MoodBarItem.PROGRESS_NONE;
                    moodBarItem.Status = MoodBarItemStatus.Processing;
                    await this.Create(moodBarItem).ConfigureAwait(false);
                    if (moodBarItem.Status == MoodBarItemStatus.Complete)
                    {
                        Logger.Write(this, LogLevel.Debug, "Creating file \"{0}\" completed successfully.", moodBarItem.FileName);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Creating file \"{0}\" failed: Unknown error.", moodBarItem.FileName);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", moodBarItem.FileName);
                    moodBarItem.Status = MoodBarItemStatus.Cancelled;
                    return;
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Creating file \"{0}\" failed: {1}", moodBarItem.FileName, e.Message);
                    moodBarItem.Status = MoodBarItemStatus.Failed;
                    moodBarItem.AddError(e.Message);
                }
                finally
                {
                    moodBarItem.Progress = MoodBarItem.PROGRESS_COMPLETE;
                }
            }, this.CancellationToken, parallelOptions).ConfigureAwait(false);
            Logger.Write(this, LogLevel.Debug, "Creation completed successfully.");
        }

        protected virtual async Task Create(MoodBarItem moodBarItem)
        {
            var success = default(bool);
            var flags = BassFlags.Decode | BassFlags.Float;
            using (var stream = this.CreateStream(moodBarItem.FileName, flags))
            {
                if (stream.IsEmpty)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create stream for file \"{0}\": Unknown error.", moodBarItem.FileName);
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Created stream for file \"{0}\": {1}", moodBarItem.FileName, stream.ChannelHandle);
                var outputStream = new BassOutputStream(null, null, stream, new PlaylistItem() { FileName = moodBarItem.FileName });
                outputStream.InitializeComponent(this.Core);
                using (var monitor = new ChannelMonitor(moodBarItem, stream))
                {
                    monitor.Start();
                    var task = this.Create(moodBarItem, outputStream);
                    if (task != null)
                    {
                        await task.ConfigureAwait(false);
                    }
                    success = true;
                }
            }
            if (this.CancellationToken.IsCancellationRequested)
            {
                moodBarItem.Status = MoodBarItemStatus.Cancelled;
            }
            else if (success)
            {
                moodBarItem.Status = MoodBarItemStatus.Complete;
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Creating file \"{0}\" failed: The format is likely not supported.", moodBarItem.FileName);
                moodBarItem.AddError("The format is likely not supported.");
                moodBarItem.AddError("Only stereo files of common sample rates are supported.");
                moodBarItem.Status = MoodBarItemStatus.Failed;
            }
        }

        protected virtual Task Create(MoodBarItem moodBarItem, IOutputStream stream)
        {
            var generator = new MoodBarGenerator();
            generator.InitializeComponent(this.Core);
            var task = default(Task);
            moodBarItem.Data = generator.Generate(stream, out task);
            return task;
        }

        public void Prune()
        {
            for (var a = this.MoodBarItems.Count - 1; a >= 0; a--)
            {
                if (this.MoodBarItems[a].Status == MoodBarItemStatus.Complete)
                {
                    this.MoodBarItems.RemoveAt(a);
                }
            }
        }

        protected virtual void OnUpdated()
        {
            if (this.Updated == null)
            {
                return;
            }
            this.Updated(this, EventArgs.Empty);
        }

        public event EventHandler Updated;
    }
}
