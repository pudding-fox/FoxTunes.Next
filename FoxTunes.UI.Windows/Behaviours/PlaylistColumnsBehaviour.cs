using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PlaylistColumnsBehaviour : StandardBehaviour, IInvocableComponent, IDisposable
    {
        const string SETTINGS = "ZZZZ";

        const int TIMEOUT = 1000;

        public PlaylistColumnsBehaviour()
        {
            this.Columns = new ConcurrentDictionary<PlaylistColumn, bool>();
            this.Debouncer = new AsyncDebouncer(TIMEOUT);
        }

        public ConcurrentDictionary<PlaylistColumn, bool> Columns { get; private set; }

        public AsyncDebouncer Debouncer { get; private set; }

        public PlaylistGridViewColumnFactory GridViewColumnFactory { get; private set; }

        public ICore Core { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.GridViewColumnFactory = ComponentRegistry.Instance.GetComponent<PlaylistGridViewColumnFactory>();
            this.GridViewColumnFactory.PositionChanged += this.OnPositionChanged;
            this.GridViewColumnFactory.WidthChanged += this.OnWidthChanged;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected virtual void OnPositionChanged(object sender, PlaylistColumn playlistColumn)
        {
            this.Update(playlistColumn, true);
        }

        protected virtual void OnWidthChanged(object sender, PlaylistColumn playlistColumn)
        {
            this.Update(playlistColumn, false);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST_HEADER;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                foreach (var playlistColumn in this.PlaylistBrowser.GetColumns())
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_PLAYLIST_HEADER,
                        Convert.ToString(playlistColumn.Id),
                        playlistColumn.Name,
                        path: "Columns",
                        attributes: playlistColumn.Enabled ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_PLAYLIST_HEADER,
                    SETTINGS,
                    Strings.General_Settings,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, SETTINGS, StringComparison.OrdinalIgnoreCase))
            {
                return Windows.ShowDialog<PlaylistSettingsDialog>(this.Core, Strings.General_Settings);
            }
            var id = default(int);
            if (!int.TryParse(component.Id, out id))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var column = this.PlaylistBrowser.GetColumns().FirstOrDefault(
                _column => _column.Id == id
            );
            if (column != null)
            {
                column.Enabled = !column.Enabled;
                this.Update(column, true);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void Update(PlaylistColumn column, bool notify)
        {
            this.Columns.AddOrUpdate(column, notify);
            this.Debouncer.Exec(this.Update);
            Logger.Write(this, LogLevel.Debug, "Queued update for column {0} => {1}.", column.Id, column.Name);
        }

        protected virtual async Task Update()
        {
            Logger.Write(this, LogLevel.Debug, "Updating {0} columns...", this.Columns.Count);
            var columns = new List<PlaylistColumn>();
            using (var database = this.DatabaseFactory.Create())
            {
                using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                    {
                        var set = database.Set<PlaylistColumn>(transaction);
                        foreach (var pair in this.Columns)
                        {
                            Logger.Write(this, LogLevel.Debug, "Updating column {0} => {1}.", pair.Key.Id, pair.Key.Name);
                            await set.AddOrUpdateAsync(pair.Key).ConfigureAwait(false);
                            if (pair.Value)
                            {
                                columns.Add(pair.Key);
                            }
                            this.Columns.TryRemove(pair.Key);
                        }
                        transaction.Commit();
                    }
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
            }
            if (columns.Any())
            {
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistColumnsUpdated, new PlaylistColumnsUpdatedSignalState(columns, DataSignalType.Updated))).ConfigureAwait(false);
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
            if (this.GridViewColumnFactory != null)
            {
                this.GridViewColumnFactory.PositionChanged -= this.OnPositionChanged;
                this.GridViewColumnFactory.WidthChanged -= this.OnWidthChanged;
            }
            if (this.Debouncer != null)
            {
                this.Debouncer.Dispose();
            }
        }

        ~PlaylistColumnsBehaviour()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
