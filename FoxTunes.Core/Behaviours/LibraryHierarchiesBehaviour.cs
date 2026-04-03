using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryHierarchiesBehaviour : StandardBehaviour, IDisposable
    {
        public ICore Core { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.LibraryUpdated:
                    if (signal.State is LibraryUpdatedSignalState state && state.LibraryItems != null && state.LibraryItems.Any())
                    {
                        return this.OnLibraryUpdated(state.LibraryItems);
                    }
                    else
                    {
                        return this.OnLibraryUpdated();
                    }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async Task OnLibraryUpdated()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var set = database.Set<LibraryItem>(transaction);
                    using (var task = new BuildLibraryHierarchiesTask(set))
                    {
                        task.InitializeComponent(this.Core);
                        await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                        await task.Run().ConfigureAwait(false);
                    }
                }
            }
        }

        protected virtual async Task OnLibraryUpdated(IEnumerable<LibraryItem> libraryItems)
        {
            using (var task = new BuildLibraryHierarchiesTask(libraryItems))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
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
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~LibraryHierarchiesBehaviour()
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
