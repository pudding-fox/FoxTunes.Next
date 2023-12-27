#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class LibraryManager : StandardManager, ILibraryManager
    {
        public LibraryManager()
        {
            this._SelectedItem = new ConcurrentDictionary<LibraryHierarchy, LibraryHierarchyNode>();
        }

        public LibraryManagerState State
        {
            get
            {
                if (global::FoxTunes.BackgroundTask.Active.Any(backgroundTask =>
                {
                    var type = backgroundTask.GetType();
                    return type == typeof(AddPathsToLibraryTask) || type == typeof(ClearLibraryTask) || type == typeof(RescanLibraryTask);
                }))
                {
                    return LibraryManagerState.Updating;
                }
                return LibraryManagerState.None;
            }
        }

        public ICore Core { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ILibraryHierarchyBrowser HierarchyBrowser { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        private LibraryHierarchy _SelectedHierarchy { get; set; }

        public LibraryHierarchy SelectedHierarchy
        {
            get
            {
                return this._SelectedHierarchy;
            }
            set
            {
                if (object.ReferenceEquals(this._SelectedHierarchy, value))
                {
                    return;
                }
                this._SelectedHierarchy = value;
                this.OnSelectedHierarchyChanged();
            }
        }

        protected virtual void OnSelectedHierarchyChanged()
        {
            this.OnSelectedItemChanged();
            if (this.SelectedHierarchyChanged != null)
            {
                this.SelectedHierarchyChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedHierarchy");
        }

        public event EventHandler SelectedHierarchyChanged;

        private ConcurrentDictionary<LibraryHierarchy, LibraryHierarchyNode> _SelectedItem { get; set; }

        public LibraryHierarchyNode SelectedItem
        {
            get
            {
                var libraryHierarchyNode = default(LibraryHierarchyNode);
                if (this.SelectedHierarchy == null || !this._SelectedItem.TryGetValue(this.SelectedHierarchy, out libraryHierarchyNode))
                {
                    return default(LibraryHierarchyNode);
                }
                return libraryHierarchyNode;
            }
            set
            {
                if (this.SelectedHierarchy == null || object.ReferenceEquals(this.SelectedItem, value))
                {
                    return;
                }
                this._SelectedItem[this.SelectedHierarchy] = value;
                this.OnSelectedItemChanged();
            }
        }

        protected virtual void OnSelectedItemChanged()
        {
            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedItem");
        }

        public event EventHandler SelectedItemChanged;

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.HierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.HierarchyBrowser.FilterChanged += this.OnFilterChanged;
            this.PlaybackManager = core.Managers.Playback;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ReportEmitter = core.Components.ReportEmitter;
            this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.HierarchiesUpdated:
                    this.Refresh();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public void Refresh()
        {
            this.RefreshSelectedHierarchy();
            this.RefreshSelectedItem();
        }

        protected virtual void RefreshSelectedHierarchy()
        {
            var selectedHierarchy = this.SelectedHierarchy;
            if (selectedHierarchy != null)
            {
                selectedHierarchy = this.HierarchyBrowser.GetHierarchies().FirstOrDefault(libraryHierarchy => libraryHierarchy.Id == selectedHierarchy.Id);
                if (selectedHierarchy != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshed selected hierarchy: {0} => {1}", selectedHierarchy.Id, selectedHierarchy.Name);
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to refresh selected hierarchy, it was removed or disabled.");
                }
            }
            if (selectedHierarchy == null)
            {
                selectedHierarchy = this.HierarchyBrowser.GetHierarchies().FirstOrDefault();
                if (selectedHierarchy != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Selected first hierarchy: {0} => {1}", selectedHierarchy.Id, selectedHierarchy.Name);
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to select a hierarchy, perhaps none are enabled?");
                }
            }
            if (object.ReferenceEquals(this.SelectedHierarchy, selectedHierarchy))
            {
                return;
            }
            this.SelectedHierarchy = selectedHierarchy;
        }

        protected virtual void RefreshSelectedItem()
        {
            if (this._SelectedItem == null)
            {
                return;
            }
            var libraryHierarchies = this._SelectedItem.Keys.ToArray();
            foreach (var libraryHierarchy in libraryHierarchies)
            {
                this.RefreshSelectedItem(libraryHierarchy);
                this.OnSelectedItemChanged();
            }
        }

        protected virtual void RefreshSelectedItem(LibraryHierarchy libraryHierarchy)
        {
            if (this._SelectedItem == null)
            {
                return;
            }
            var selectedItem = default(LibraryHierarchyNode);
            if (!this._SelectedItem.TryGetValue(libraryHierarchy, out selectedItem))
            {
                return;
            }
            if (selectedItem != null)
            {
                selectedItem = this.HierarchyBrowser.GetNode(libraryHierarchy, selectedItem);
                if (selectedItem != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Refreshed selected item: {0} => {1}", selectedItem.Id, selectedItem.Value);
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to refresh selected item, it was removed.");
                }
            }
            if (selectedItem == null)
            {
                selectedItem = this.HierarchyBrowser.GetNodes(libraryHierarchy).FirstOrDefault();
                if (selectedItem != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Selected first item: {0} => {1}", selectedItem.Id, selectedItem.Value);
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to select an item, perhaps none are available?");
                }
            }
            if (object.ReferenceEquals(this._SelectedItem[libraryHierarchy], selectedItem))
            {
                return;
            }
            this._SelectedItem[libraryHierarchy] = selectedItem;
        }

        public async Task Add(IEnumerable<string> paths)
        {
            using (var task = new AddPathsToLibraryTask(paths))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport(task.Warnings);
            }
        }

        public async Task Clear(LibraryItemStatus? status)
        {
            using (var task = new ClearLibraryTask(status))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Rescan(bool force)
        {
            using (var task = new RescanLibraryTask(force))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport(task.Warnings);
            }
        }

        public async Task SetStatus(LibraryItemStatus status)
        {
            using (var task = new UpdateLibraryTask(status))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task SetStatus(IEnumerable<LibraryItem> items, LibraryItemStatus status)
        {
            using (var task = new UpdateLibraryTask(items, status))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        protected virtual void OnReport(IDictionary<LibraryItem, IList<string>> warnings)
        {
            if (this.State.HasFlag(LibraryManagerState.Updating))
            {
                //Another task was queued.
                return;
            }
            var report = new LibraryManagerReport(warnings);
            report.InitializeComponent(this.Core);
            this.ReportEmitter.Send(report);
        }

        public bool CanHandle(string path, FileActionType type)
        {
            if (type != FileActionType.Library)
            {
                return false;
            }
            if (!Directory.Exists(path) && !this.PlaybackManager.IsSupported(path))
            {
                return false;
            }
            return true;
        }

        public Task Handle(IEnumerable<string> paths, FileActionType type)
        {
            switch (type)
            {
                case FileActionType.Library:
                    return this.Add(paths);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Handle(IEnumerable<string> paths, int index, FileActionType type)
        {
            throw new NotImplementedException();
        }

        protected override void OnDisposing()
        {
            if (this.HierarchyBrowser != null)
            {
                this.HierarchyBrowser.FilterChanged -= this.OnFilterChanged;
            }
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }

        public class LibraryManagerReport : ReportComponent
        {
            public LibraryManagerReport(IDictionary<LibraryItem, IList<string>> warnings)
            {
                this.Warnings = warnings;
            }

            public IDictionary<LibraryItem, IList<string>> Warnings { get; private set; }

            public IDatabaseFactory DatabaseFactory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.DatabaseFactory = core.Factories.Database;
                base.InitializeComponent(core);
            }

            public override string Title
            {
                get
                {
                    return "Library Status";
                }
            }

            public override string Description
            {
                get
                {
                    var builder = new StringBuilder();
                    foreach (var libraryItem in this.Warnings.Keys)
                    {
                        var warnings = this.Warnings[libraryItem];
                        builder.Append(libraryItem.FileName);
                        builder.AppendLine(" -> Warning");
                        foreach (var warning in warnings)
                        {
                            builder.AppendLine('\t' + warning);
                        }
                    }
                    return builder.ToString();
                }
            }

            public override string[] Headers
            {
                get
                {
                    return new[]
                    {
                        "Path",
                        "Tracks"
                    };
                }
            }

            public override IEnumerable<IReportComponentRow> Rows
            {
                get
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var libraryRoots = database.Set<LibraryRoot>(transaction);
                            foreach (var libraryRoot in libraryRoots)
                            {
                                yield return this.GetRow(database, libraryRoot, transaction);
                            }
                        }
                    }
                }
            }

            protected virtual IReportComponentRow GetRow(IDatabaseComponent database, LibraryRoot libraryRoot, ITransactionSource transaction)
            {
                var table = database.Tables.LibraryItem;
                var builder = database.QueryFactory.Build();
                builder.Output.AddFunction(QueryFunction.Count, builder.Output.CreateOperator(QueryOperator.Star));
                builder.Source.AddTable(table);
                builder.Filter.Add().With(binary =>
                {
                    binary.Left = binary.CreateColumn(table.Column("DirectoryName"));
                    binary.Operator = binary.CreateOperator(QueryOperator.Match);
                    binary.Right = binary.CreateConstant(libraryRoot.DirectoryName + "%");
                });
                var count = database.ExecuteScalar<long>(builder.Build(), transaction);
                return new LibraryManagerReportRow(libraryRoot.DirectoryName, count);
            }

            private class LibraryManagerReportRow : ReportComponentRow
            {
                public LibraryManagerReportRow(string directoryName, long count)
                {
                    this.DirectoryName = directoryName;
                    this.Count = count;
                }

                public string DirectoryName { get; private set; }

                public long Count { get; private set; }

                public override string[] Values
                {
                    get
                    {
                        return new[]
                        {
                            this.DirectoryName,
                            Convert.ToString(this.Count)
                        };
                    }
                }
            }
        }
    }
}
