using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MoodBarMonitor : PopulatorBase, IMoodBarMonitor
    {
        public static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);

        public MoodBarMonitor(IMoodBar MoodBar, IEnumerable<MoodBarGenerator.MoodBarGeneratorData> generatorDatas, bool reportProgress, CancellationToken cancellationToken) : base(reportProgress)
        {
            this.MoodBar = MoodBar;
            this.GeneratorDatas = generatorDatas;
            this.CancellationToken = cancellationToken;
        }

        public IMoodBar MoodBar { get; private set; }

        public IEnumerable<MoodBarGenerator.MoodBarGeneratorData> GeneratorDatas { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public MoodBarCache Cache { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Cache = ComponentRegistry.Instance.GetComponent<MoodBarCache>();
            base.InitializeComponent(core);
        }

        public Task Create()
        {
#if NET40
            var task = TaskEx.Run(() =>
#else
            var task = Task.Run(() =>
#endif
            {
                this.MoodBar.Create();
            });
#if NET40
            return TaskEx.WhenAll(task, this.Monitor(task));
#else
            return Task.WhenAll(task, this.Monitor(task));
#endif
        }

        protected virtual async Task Monitor(Task task)
        {
            this.Name = "Creating files";
            var persisted = new HashSet<MoodBarItem>();
            while (!task.IsCompleted)
            {
                if (this.CancellationToken.IsCancellationRequested)
                {
                    Logger.Write(this, LogLevel.Debug, "Requesting cancellation from moodbar.");
                    this.MoodBar.Cancel();
                    this.Name = "Cancelling";
                    break;
                }
                this.MoodBar.Update();
                var position = 0;
                var count = 0;
                var builder = new StringBuilder();
                foreach (var moodBarItem in this.MoodBar.MoodBarItems)
                {
                    var generatorData = this.GeneratorDatas.FirstOrDefault(_generatorData => string.Equals(_generatorData.FileName, moodBarItem.FileName, StringComparison.OrdinalIgnoreCase));
                    if (moodBarItem.Data != null && generatorData != null)
                    {
                        generatorData.Data = moodBarItem.Data.Data;
                        generatorData.Position = moodBarItem.Data.Position;
                        generatorData.Capacity = moodBarItem.Data.Capacity;
                        generatorData.Peak = moodBarItem.Data.Peak;
                        generatorData.Update();
                    }
                    if (moodBarItem.Status == MoodBarItemStatus.Complete)
                    {
                        if (persisted.Add(moodBarItem))
                        {
                            this.Cache.Save(moodBarItem.FileName, generatorData);
                        }
                    }
                    position += moodBarItem.Progress;
                    count += MoodBarItem.PROGRESS_COMPLETE;
                    if (moodBarItem.Status == MoodBarItemStatus.Processing && moodBarItem.Progress != MoodBarItem.PROGRESS_COMPLETE)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append(", ");
                        }
                        builder.Append(Path.GetFileName(moodBarItem.FileName));
                    }
                }
                if (builder.Length > 0)
                {
                    this.Description = builder.ToString();
                }
                else
                {
                    this.Description = "Waiting for moodbar";
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
                Logger.Write(this, LogLevel.Debug, "Waiting for moodbar to complete.");
                this.MoodBar.Update();
#if NET40
                await TaskEx.Delay(INTERVAL).ConfigureAwait(false);
#else
                await Task.Delay(INTERVAL).ConfigureAwait(false);
#endif
            }
        }
    }
}
