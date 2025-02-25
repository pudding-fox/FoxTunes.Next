using FoxTunes.Interfaces;
using ManagedBass.Memory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassMemoryLoadingBehaviour : StandardBehaviour, IDisposable
    {
        public ConcurrentDictionary<string, LoadingTask> Tasks { get; private set; }

        public BassMemoryProgressHandler Handler;

        public BassMemoryLoadingBehaviour()
        {
            this.Tasks = new ConcurrentDictionary<string, LoadingTask>(StringComparer.OrdinalIgnoreCase);
            this.Handler = new BassMemoryProgressHandler(this.UpdateProgress);
        }

        public IBassLoader BassLoader { get; private set; }

        public ICore Core { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.BassLoader = ComponentRegistry.Instance.GetComponent<IBassLoader>();
            if (this.BassLoader.IsLoaded)
            {
                this.OnIsLoadedChanged(this, EventArgs.Empty);
            }
            else
            {
                this.BassLoader.IsLoadedChanged += this.OnIsLoadedChanged;
            }
            this.Core = core;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        protected virtual void OnIsLoadedChanged(object sender, EventArgs e)
        {
            if (this.BassLoader.IsLoaded)
            {
                try
                {
                    BassMemory.Progress(this.Handler);
                    Logger.Write(this, LogLevel.Debug, "Registered progress handler.");
                }
                catch (Exception ex)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to register progress handler: {0}", ex.Message);
                }
            }
        }

        protected virtual void UpdateProgress(ref BassMemoryProgress progress)
        {
            switch (progress.Type)
            {
                case BassMemoryProgressType.Begin:
                    this.OnBegin(progress.File);
                    break;
                case BassMemoryProgressType.Update:
                    var cancel = default(bool);
                    this.OnUpdate(progress.File, progress.Position, progress.Length, out cancel);
                    if (cancel)
                    {
                        progress.Cancel = true;
                    }
                    break;
                case BassMemoryProgressType.End:
                    this.OnEnd(progress.File);
                    break;
            }
        }

        protected virtual void OnBegin(string fileName)
        {
            var loadingTask = new LoadingTask(fileName);
            if (!this.Tasks.TryAdd(fileName, loadingTask))
            {
                return;
            }
            loadingTask.InitializeComponent(this.Core);
            var task = this.BackgroundTaskEmitter.Send(loadingTask);
        }

        protected virtual void OnUpdate(string fileName, long position, long length, out bool cancel)
        {
            var loadingTask = default(LoadingTask);
            if (!this.Tasks.TryGetValue(fileName, out loadingTask))
            {
                cancel = false;
                return;
            }
            loadingTask.Update(position, length);
            cancel = loadingTask.IsCancellationRequested;
        }

        protected virtual void OnEnd(string fileName)
        {
            var loadingTask = default(LoadingTask);
            if (!this.Tasks.TryRemove(fileName, out loadingTask))
            {
                return;
            }
            loadingTask.Dispose();
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
            if (this.Tasks != null)
            {
                foreach (var pair in this.Tasks)
                {
                    pair.Value.Dispose();
                }
            }
            if (this.BassLoader != null)
            {
                this.BassLoader.IsLoadedChanged -= this.OnIsLoadedChanged;
            }
        }

        ~BassMemoryLoadingBehaviour()
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

        public class LoadingTask : BackgroundTask
        {
            public const string ID = "1C9575FB-6CE0-4EE9-9A8E-0D337182A29E";

            private LoadingTask() : base(ID)
            {
                this.Name = Strings.LoadingTask_Name;
                this.Count = 100;
            }

            public LoadingTask(string fileName) : this()
            {
                this.FileName = fileName;
                this.Description = Path.GetFileName(fileName); ;
            }

            public string FileName { get; private set; }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public override bool Cancellable
            {
                get
                {
                    return true;
                }
            }

            protected override Task OnRun()
            {
                throw new NotImplementedException();
            }

            public void Update(long position, long length)
            {
                var percent = Convert.ToInt32(((float)position / length) * 100);
                this.Position = percent;
            }
        }
    }
}
