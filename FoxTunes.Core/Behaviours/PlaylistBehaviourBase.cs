using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class PlaylistBehaviourBase : StandardBehaviour, IDisposable
    {
        public abstract Func<Playlist, bool> Predicate { get; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.DatabaseFactory = core.Factories.Database;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            base.InitializeComponent(core);
        }

        protected virtual IEnumerable<Playlist> GetPlaylists()
        {
            return this.PlaylistBrowser.GetPlaylists().Where(this.Predicate);
        }

        protected virtual PlaylistConfig GetConfig(Playlist playlist)
        {
            return new PlaylistConfig(playlist);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null && state.Playlists.Any())
            {
                //Nothing to do.
            }
            else
            {
                this.Dispatch(() => this.Refresh(true));
            }
        }

        protected virtual async Task Refresh(bool force)
        {
            foreach (var playlist in this.GetPlaylists())
            {
                await this.Refresh(playlist, force).ConfigureAwait(false);
            }
        }

        public abstract Task Refresh(Playlist playlist, bool force);

        protected virtual async Task Update(Playlist playlist)
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                    {
                        var set = database.Set<Playlist>(transaction);
                        await set.AddOrUpdateAsync(playlist).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }))
                {
                    await task.Run().ConfigureAwait(false);
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
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~PlaylistBehaviourBase()
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
