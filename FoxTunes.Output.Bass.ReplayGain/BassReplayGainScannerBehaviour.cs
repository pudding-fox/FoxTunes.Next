using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassReplayGainScannerBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public const string SCAN_TRACKS = "HHHH";

        public const string SCAN_ALBUMS = "IIII";

        public const string CLEAR = "JJJJ";

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IPlaylistCache PlaylistCache { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public BooleanConfigurationElement WriteTags { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.PlaylistManager = core.Managers.Playlist;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.MetaDataManager = core.Managers.MetaData;
            this.PlaylistCache = core.Components.PlaylistCache;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.ReportEmitter = core.Components.ReportEmitter;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassReplayGainBehaviourConfiguration.ENABLED
            );
            this.WriteTags = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassReplayGainScannerBehaviourConfiguration.WRITE_TAGS
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_LIBRARY;
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled.Value)
                {
                    if (this.LibraryManager.SelectedItem != null)
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, SCAN_TRACKS, Strings.BassReplayGainScannerBehaviour_ScanTracks, path: Strings.BassReplayGainScannerBehaviour_Path);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, SCAN_ALBUMS, Strings.BassReplayGainScannerBehaviour_ScanAlbums, path: Strings.BassReplayGainScannerBehaviour_Path);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, CLEAR, Strings.BassReplayGainScannerBehaviour_ClearData, path: Strings.BassReplayGainScannerBehaviour_Path, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                    }
                    if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SCAN_TRACKS, Strings.BassReplayGainScannerBehaviour_ScanTracks, path: Strings.BassReplayGainScannerBehaviour_Path);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SCAN_ALBUMS, Strings.BassReplayGainScannerBehaviour_ScanAlbums, path: Strings.BassReplayGainScannerBehaviour_Path);
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, CLEAR, Strings.BassReplayGainScannerBehaviour_ClearData, path: Strings.BassReplayGainScannerBehaviour_Path, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case SCAN_TRACKS:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.ScanLibrary(ReplayGainMode.Track);
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.ScanPlaylist(ReplayGainMode.Track);
                    }
                    break;
                case SCAN_ALBUMS:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.ScanLibrary(ReplayGainMode.Album);
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.ScanPlaylist(ReplayGainMode.Album);
                    }
                    break;
                case CLEAR:
                    switch (component.Category)
                    {
                        case InvocationComponent.CATEGORY_LIBRARY:
                            return this.ClearLibrary(MetaDataUpdateType.User);
                        case InvocationComponent.CATEGORY_PLAYLIST:
                            return this.ClearPlaylist(MetaDataUpdateType.User);
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassReplayGainScannerBehaviourConfiguration.GetConfigurationSections();
        }

        public Task ScanLibrary(ReplayGainMode mode)
        {
            if (this.LibraryManager == null || this.LibraryManager.SelectedItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(this.LibraryManager.SelectedItem);
            if (!libraryItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Scan(libraryItems, mode);
        }

        public Task ScanPlaylist(ReplayGainMode mode)
        {
            if (this.PlaylistManager.SelectedItems == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var playlistItems = this.PlaylistManager.SelectedItems.ToArray();
            if (!playlistItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Scan(playlistItems, mode);
        }

        public async Task Scan(IFileData[] fileDatas, ReplayGainMode mode)
        {
            using (var task = new ScanTask(this, fileDatas, mode))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.OnReport(fileDatas, task.ScannerItems);
            }
        }

        public Task ClearLibrary(MetaDataUpdateType updateType)
        {
            if (this.LibraryManager == null || this.LibraryManager.SelectedItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var libraryItems = this.LibraryHierarchyBrowser.GetItems(this.LibraryManager.SelectedItem);
            if (!libraryItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Clear(libraryItems, updateType);
        }

        public Task ClearPlaylist(MetaDataUpdateType updateType)
        {
            if (this.PlaylistManager.SelectedItems == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var playlistItems = this.PlaylistManager.SelectedItems.ToArray();
            if (!playlistItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Clear(playlistItems, updateType);
        }

        public async Task Clear(IFileData[] fileDatas, MetaDataUpdateType updateType)
        {
            var names = new[]
            {
                CommonMetaData.ReplayGainAlbumGain,
                CommonMetaData.ReplayGainAlbumPeak,
                CommonMetaData.ReplayGainTrackGain,
                CommonMetaData.ReplayGainTrackPeak
            };
            foreach (var fileData in fileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    foreach (var metaDataItem in fileData.MetaDatas)
                    {
                        if (!names.Contains(metaDataItem.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        //This will be converted to NaN when written.
                        metaDataItem.Value = string.Empty;
                    }
                }
            }
            var flags = MetaDataUpdateFlags.None;
            if (this.WriteTags.Value)
            {
                flags |= MetaDataUpdateFlags.WriteToFiles;
            }
            await this.MetaDataManager.Save(
               fileDatas,
               names,
               updateType,
               flags
            ).ConfigureAwait(false);
        }

        protected virtual void OnReport(IFileData[] fileDatas, ScannerItem[] scannerItems)
        {
            var report = new BassReplayGainScannerReport(fileDatas, scannerItems);
            report.InitializeComponent(this.Core);
            this.ReportEmitter.Send(report);
        }

        private class ScanTask : BackgroundTask
        {
            public const string ID = "1112F788-99E4-4019-84D9-55B99BD2093E";

            public static readonly IBassReplayGainScannerFactory ScannerFactory = ComponentRegistry.Instance.GetComponent<IBassReplayGainScannerFactory>();

            private ScanTask() : base(ID)
            {
                this.CancellationToken = new CancellationToken();
            }

            public ScanTask(BassReplayGainScannerBehaviour behaviour, IFileData[] fileDatas, ReplayGainMode mode) : this()
            {
                this.Behaviour = behaviour;
                this.FileDatas = fileDatas;
                this.ScannerItems = fileDatas
                    .OrderBy(fileData => fileData.FileName)
                    .Select(fileData => ScannerItem.FromFileData(fileData, mode))
                    .ToArray();
            }

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

            public CancellationToken CancellationToken { get; private set; }

            public BassReplayGainScannerBehaviour Behaviour { get; private set; }

            public IFileData[] FileDatas { get; private set; }

            public ScannerItem[] ScannerItems { get; private set; }

            public ICore Core { get; private set; }

            public ISignalEmitter SignalEmitter { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.Core = core;
                this.SignalEmitter = core.Components.SignalEmitter;
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                Logger.Write(this, LogLevel.Debug, "Creating scanner.");
                using (var scanner = ScannerFactory.CreateScanner(this.ScannerItems))
                {
                    Logger.Write(this, LogLevel.Debug, "Starting scanner.");
                    using (var monitor = new BassReplayGainScannerMonitor(scanner, this.Visible, this.CancellationToken))
                    {
                        await this.WithSubTask(monitor,
                            () => monitor.Scan()
                        ).ConfigureAwait(false);
                        this.ScannerItems = monitor.ScannerItems.Values.ToArray();
                    }
                }
                Logger.Write(this, LogLevel.Debug, "Scanner completed successfully.");
            }

            protected override async Task OnCompleted()
            {
                await base.OnCompleted().ConfigureAwait(false);
                await this.WriteTags().ConfigureAwait(false);
            }

            protected virtual async Task WriteTags()
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var scannerItem in this.ScannerItems)
                {
                    if (scannerItem.Status != ScannerItemStatus.Complete)
                    {
                        continue;
                    }
                    var fileData = this.GetFileData(scannerItem);
                    lock (fileData.MetaDatas)
                    {
                        var metaDatas = fileData.MetaDatas.ToDictionary(
                            element => element.Name,
                            StringComparer.OrdinalIgnoreCase
                        );
                        var metaDataItem = default(MetaDataItem);
                        if (scannerItem.GroupGain != 0)
                        {
                            if (!metaDatas.TryGetValue(CommonMetaData.ReplayGainAlbumGain, out metaDataItem))
                            {
                                metaDataItem = new MetaDataItem(CommonMetaData.ReplayGainAlbumGain, MetaDataItemType.Tag);
                                fileData.MetaDatas.Add(metaDataItem);
                            }
                            metaDataItem.Value = Convert.ToString(scannerItem.GroupGain);
                            names.Add(CommonMetaData.ReplayGainAlbumGain);
                        }
                        if (scannerItem.GroupPeak != 0)
                        {
                            if (!metaDatas.TryGetValue(CommonMetaData.ReplayGainAlbumPeak, out metaDataItem))
                            {
                                metaDataItem = new MetaDataItem(CommonMetaData.ReplayGainAlbumPeak, MetaDataItemType.Tag);
                                fileData.MetaDatas.Add(metaDataItem);
                            }
                            metaDataItem.Value = Convert.ToString(scannerItem.GroupPeak);
                            names.Add(CommonMetaData.ReplayGainAlbumPeak);
                        }
                        if (scannerItem.ItemGain != 0)
                        {
                            if (!metaDatas.TryGetValue(CommonMetaData.ReplayGainTrackGain, out metaDataItem))
                            {
                                metaDataItem = new MetaDataItem(CommonMetaData.ReplayGainTrackGain, MetaDataItemType.Tag);
                                fileData.MetaDatas.Add(metaDataItem);
                            }
                            metaDataItem.Value = Convert.ToString(scannerItem.ItemGain);
                            names.Add(CommonMetaData.ReplayGainTrackGain);
                        }
                        if (scannerItem.ItemPeak != 0)
                        {
                            if (!metaDatas.TryGetValue(CommonMetaData.ReplayGainTrackPeak, out metaDataItem))
                            {
                                metaDataItem = new MetaDataItem(CommonMetaData.ReplayGainTrackPeak, MetaDataItemType.Tag);
                                fileData.MetaDatas.Add(metaDataItem);
                            }
                            metaDataItem.Value = Convert.ToString(scannerItem.ItemPeak);
                            names.Add(CommonMetaData.ReplayGainTrackPeak);
                        }
                    }
                }
                if (names.Any())
                {
                    var fileDatas = this.ScannerItems
                        .Where(scannerItem => scannerItem.Status == ScannerItemStatus.Complete)
                        .Select(scannerItem => this.GetFileData(scannerItem))
                        .ToArray();
                    var flags = MetaDataUpdateFlags.None;
                    if (this.Behaviour.WriteTags.Value)
                    {
                        flags |= MetaDataUpdateFlags.WriteToFiles;
                    }
                    await this.Behaviour.MetaDataManager.Save(
                        fileDatas,
                        names,
                        MetaDataUpdateType.System,
                        flags
                    ).ConfigureAwait(false);
                }
            }

            protected virtual IFileData GetFileData(ScannerItem scannerItem)
            {
                return this.FileDatas.FirstOrDefault(fileData => string.Equals(fileData.FileName, scannerItem.FileName, StringComparison.OrdinalIgnoreCase));
            }

            protected override void OnCancellationRequested()
            {
                this.CancellationToken.Cancel();
                base.OnCancellationRequested();
            }
        }
    }
}
