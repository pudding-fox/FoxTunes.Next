using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class MetaDataManager : StandardManager, IMetaDataManager
    {
        public ICore Core { get; private set; }

        public IHierarchyManager HierarchyManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.HierarchyManager = core.Managers.Hierarchy;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ReportEmitter = core.Components.ReportEmitter;
            base.InitializeComponent(core);
        }

        public async Task Rescan(IEnumerable<IFileData> fileDatas, MetaDataUpdateFlags flags)
        {
            var libraryItems = fileDatas.OfType<LibraryItem>().ToArray();
            var playlistItems = fileDatas.OfType<PlaylistItem>().ToArray();
            if (libraryItems.Any())
            {
                using (var task = new RefreshLibraryMetaDataTask(libraryItems))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
            if (playlistItems.Any())
            {
                using (var task = new RefreshPlaylistMetaDataTask(playlistItems, MetaDataUpdateType.System))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                }
            }
            if (flags.HasFlag(MetaDataUpdateFlags.RefreshHierarchies))
            {
                await this.HierarchyManager.Refresh(fileDatas, Enumerable.Empty<string>()).ConfigureAwait(false);
            }
        }

        public async Task Save(IEnumerable<IFileData> fileDatas, IEnumerable<string> names, MetaDataUpdateType updateType, MetaDataUpdateFlags flags)
        {
            var libraryItems = fileDatas.OfType<LibraryItem>().ToArray();
            var playlistItems = fileDatas.OfType<PlaylistItem>().ToArray();
            if (libraryItems.Any())
            {
                using (var task = new WriteLibraryMetaDataTask(libraryItems, names, updateType, flags))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                    if (flags.HasFlag(MetaDataUpdateFlags.ShowReport) && task.Errors.Any())
                    {
                        this.OnReport(libraryItems, task.Errors);
                    }
                }
            }
            if (playlistItems.Any())
            {
                using (var task = new WritePlaylistMetaDataTask(playlistItems, names, updateType, flags))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                    if (flags.HasFlag(MetaDataUpdateFlags.ShowReport) && task.Errors.Any())
                    {
                        this.OnReport(playlistItems, task.Errors);
                    }
                }
            }
            if (flags.HasFlag(MetaDataUpdateFlags.RefreshHierarchies))
            {
                await this.HierarchyManager.Refresh(fileDatas, names).ConfigureAwait(false);
            }
        }

        public async Task Synchronize()
        {
            using (var task = new SynchronizeMetaDataTask())
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task<bool> Synchronize(IEnumerable<IFileData> fileDatas, params string[] names)
        {
            var libraryItems = fileDatas.OfType<LibraryItem>().ToArray();
            var playlistItems = fileDatas.OfType<PlaylistItem>().ToArray();
            var result = true;
            if (libraryItems.Any())
            {
                using (var task = new SynchronizeLibraryMetaDataTask(libraryItems, names))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                    if (task.Errors.Any())
                    {
                        result = false;
                    }
                }
            }
            if (playlistItems.Any())
            {
                using (var task = new SynchronizePlaylistMetaDataTask(playlistItems, names))
                {
                    task.InitializeComponent(this.Core);
                    await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                    await task.Run().ConfigureAwait(false);
                    if (task.Errors.Any())
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        protected virtual void OnReport(LibraryItem[] libraryItems, IDictionary<LibraryItem, IList<string>> errors)
        {
            var report = new MetaDataManagerReport<LibraryItem>(libraryItems, errors);
            report.InitializeComponent(this.Core);
            this.ReportEmitter.Send(report);
        }

        protected virtual void OnReport(PlaylistItem[] playlistItems, IDictionary<PlaylistItem, IList<string>> errors)
        {
            var report = new MetaDataManagerReport<PlaylistItem>(playlistItems, errors);
            report.InitializeComponent(this.Core);
            this.ReportEmitter.Send(report);
        }

        public class MetaDataManagerReport<T> : ReportComponent where T : IFileData
        {
            public MetaDataManagerReport(T[] sequence, IDictionary<T, IList<string>> errors)
            {
                this.Sequence = sequence;
                this.Errors = errors;
            }

            public T[] Sequence { get; private set; }

            public IDictionary<T, IList<string>> Errors { get; private set; }

            public override string Title
            {
                get
                {
                    return "Meta Data Writer";
                }
            }

            public override string Description
            {
                get
                {
                    return string.Join(
                        Environment.NewLine,
                        this.Sequence.Select(
                            element => this.GetDescription(element)
                        )
                    );
                }
            }

            protected virtual string GetDescription(T element)
            {
                var builder = new StringBuilder();
                var errors = default(IList<string>);
                builder.Append(element.FileName);
                if (this.Errors.TryGetValue(element, out errors))
                {
                    builder.AppendLine(" -> Error");
                    foreach (var error in errors)
                    {
                        builder.AppendLine('\t' + error);
                    }
                }
                else
                {
                    builder.AppendLine(" -> OK");
                }
                return builder.ToString();
            }

            public override string[] Headers
            {
                get
                {
                    return new[]
                    {
                        "Path",
                        "Status"
                    };
                }
            }

            public override IEnumerable<IReportComponentRow> Rows
            {
                get
                {
                    return this.Sequence.Select(element => new MetaDataManagerReportRow(this, element, this.Errors));
                }
            }

            public IFileSystemBrowser FileSystemBrowser { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.FileSystemBrowser = core.Components.FileSystemBrowser;
                base.InitializeComponent(core);
            }

            public class MetaDataManagerReportRow : ReportComponentRow
            {
                public MetaDataManagerReportRow(MetaDataManagerReport<T> report, T element, IDictionary<T, IList<string>> errors)
                {
                    this.Report = report;
                    this.Element = element;
                    this.Errors = errors;
                }

                public MetaDataManagerReport<T> Report { get; private set; }

                public T Element { get; private set; }

                public IDictionary<T, IList<string>> Errors { get; private set; }

                public override string[] Values
                {
                    get
                    {
                        return new[]
                        {
                            this.Element.FileName,
                            this.Errors.ContainsKey(this.Element) ?
                                "Error" :
                                "OK"
                        };
                    }
                }

                public override IEnumerable<string> InvocationCategories
                {
                    get
                    {
                        yield return InvocationComponent.CATEGORY_REPORT;
                    }
                }

                public override IEnumerable<IInvocationComponent> Invocations
                {
                    get
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACTIVATE, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                    }
                }

                public override Task InvokeAsync(IInvocationComponent component)
                {
                    switch (component.Id)
                    {
                        case ACTIVATE:
                            this.Report.FileSystemBrowser.Select(this.Element.FileName);
                            break;
                    }
                    return base.InvokeAsync(component);
                }
            }
        }
    }
}
