using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class PlaybackManager : StandardManager, IPlaybackManager
    {
        public ICore Core { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStreamQueue OutputStreamQueue { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Output = core.Components.Output;
            this.Output.IsStartedChanged += this.OnIsStartedChanged;
            this.OutputStreamQueue = core.Components.OutputStreamQueue;
            this.OutputStreamQueue.Dequeued += this.OutputStreamQueueDequeued;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            base.InitializeComponent(core);
        }

        protected virtual async void OnIsStartedChanged(object sender, AsyncEventArgs e)
        {
            using (e.Defer())
            {
                await this.Unload().ConfigureAwait(false);
            }
        }

        protected virtual async void OutputStreamQueueDequeued(object sender, OutputStreamQueueEventArgs e)
        {
            using (e.Defer())
            {
                var exception = default(Exception);
                try
                {
                    Logger.Write(this, LogLevel.Debug, "Output stream is about to change, pre-empting the next stream: {0} => {1}", e.OutputStream.Id, e.OutputStream.FileName);
                    if (!await this.Output.Preempt(e.OutputStream).ConfigureAwait(false))
                    {
                        Logger.Write(this, LogLevel.Debug, "Preempt failed for stream: {0} => {1}", e.OutputStream.Id, e.OutputStream.FileName);
                    }
                    Logger.Write(this, LogLevel.Debug, "Output stream de-queued, loading it: {0} => {1}", e.OutputStream.Id, e.OutputStream.FileName);
                    await this.SetCurrentStream(e.OutputStream).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                if (exception != null)
                {
                    await this.ErrorEmitter.Send(this, exception).ConfigureAwait(false);
                }
            }
        }

        public bool IsSupported(string fileName)
        {
            return this.Output.IsSupported(fileName);
        }

        private IOutputStream _CurrentStream { get; set; }

        public IOutputStream CurrentStream
        {
            get
            {
                return this._CurrentStream;
            }
        }

        protected virtual async Task SetCurrentStream(IOutputStream stream)
        {
            var currentStream = this.CurrentStream;
            if (currentStream != null)
            {
                if (object.ReferenceEquals(currentStream, stream))
                {
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Unloading current stream: {0} => {1}", currentStream.Id, currentStream.FileName);
                this.OnCurrentStreamChanging();
                this._CurrentStream = stream;
                await this.Unload(currentStream).ConfigureAwait(false);
            }
            else
            {
                this.OnCurrentStreamChanging();
                this._CurrentStream = stream;
            }
            if (stream != null)
            {
                Logger.Write(this, LogLevel.Debug, "Playing stream: {0} => {1}", stream.Id, stream.FileName);
                try
                {
                    await stream.Play().ConfigureAwait(false);
                }
                catch
                {
                    await this.Unload(stream).ConfigureAwait(false);
                    throw;
                }
            }
            this.OnCurrentStreamChanged();
        }


        protected virtual void OnCurrentStreamChanging()
        {
            if (this.CurrentStream != null)
            {
                this.CurrentStream.Ending -= this.OnEnding;
                this.CurrentStream.Ended -= this.OnEnded;
            }
        }

        protected virtual void OnCurrentStreamChanged()
        {
            if (this.CurrentStream != null)
            {
                this.CurrentStream.Ending += this.OnEnding;
                this.CurrentStream.Ended += this.OnEnded;
            }
            if (this.CurrentStreamChanged != null)
            {
                this.CurrentStreamChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentStream");
        }

        public event EventHandler CurrentStreamChanged;

        protected virtual void OnEnding(object sender, EventArgs e)
        {
            if (this.Ending != null)
            {
                this.Ending(this, EventArgs.Empty);
            }
        }

        public event EventHandler Ending;

        protected virtual void OnEnded(object sender, EventArgs e)
        {
            if (this.Ended != null)
            {
                this.Ended(this, EventArgs.Empty);
            }
        }

        public event EventHandler Ended;

        public async Task Load(PlaylistItem playlistItem, bool immediate)
        {
            using (var task = new LoadOutputStreamTask(playlistItem, immediate))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        protected virtual Task Unload()
        {
            if (this.CurrentStream == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.SetCurrentStream(null);
        }

        public async Task Unload(IOutputStream outputStream)
        {
            using (var task = new UnloadOutputStreamTask(outputStream))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Stop()
        {
            if (!this.Output.IsStarted)
            {
                return;
            }
            await this.Output.Shutdown().ConfigureAwait(false);
        }

        protected override async void OnDisposing()
        {
            await this.Unload().ConfigureAwait(false);
            if (this.Output != null)
            {
                this.Output.IsStartedChanged -= this.OnIsStartedChanged;
            }
            if (this.OutputStreamQueue != null)
            {
                this.OutputStreamQueue.Dequeued -= this.OutputStreamQueueDequeued;
            }
            base.OnDisposing();
        }
    }
}
