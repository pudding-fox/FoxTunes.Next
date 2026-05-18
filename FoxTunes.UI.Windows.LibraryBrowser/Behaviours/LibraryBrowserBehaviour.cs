using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryBrowserBehaviour : StandardBehaviour, IDisposable
    {
        public LibraryBrowserBehaviour()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim Semaphore { get; private set; }

        public ICore Core { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.HierarchiesUpdated:
                    return this.Refresh();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task Refresh()
        {
            if (!await this.Semaphore.WaitAsync(0).ConfigureAwait(false))
            {
                return;
            }
            try
            {
                using (var task = new DiscogsFetchArtistTask())
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    this.Semaphore.Release();
                }
            }
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
            if (this.Semaphore != null)
            {
                this.Semaphore.Dispose();
            }
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~LibraryBrowserBehaviour()
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
