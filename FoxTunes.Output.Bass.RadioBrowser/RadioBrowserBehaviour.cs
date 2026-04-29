using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using RadioBrowserWrapper;
using RadioBrowserWrapper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class RadioBrowserBehaviour : StandardBehaviour, IInvocableComponent
    {
        const string SEARCH = "AAAA";

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.PlaylistManager = core.Managers.Playlist;
            this.UserInterface = core.Components.UserInterface;
            this.ReportEmitter = core.Components.ReportEmitter;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, SEARCH, Strings.RadioBrowserBehaviour_Search, path: Strings.RadioBrowserBehaviour_Path);
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case SEARCH:
                    return this.Search();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task Search()
        {
            var search = this.UserInterface.Prompt(Strings.RadioBrowserBehaviour_Search);
            if (string.IsNullOrEmpty(search))
            {
                return;
            }
            var browser = new RadioBrowser();
            var searchOptions = new AdvancedStationSearchOptions()
            {
                Name = search
            };
            var stations = await browser.SearchStationsAsync(searchOptions).ConfigureAwait(false);
            var report = new RadioBrowserReport(this, stations.ToArray());
            await this.ReportEmitter.Send(report).ConfigureAwait(false);
        }

        public Task AddStationsToPlaylist(IEnumerable<Station> stations)
        {
            var playlist = this.PlaylistManager.SelectedPlaylist;
            return this.AddStationsToPlaylist(playlist, stations);
        }


        public Task AddStationsToPlaylist(Playlist playlist, IEnumerable<Station> stations)
        {
            var index = this.PlaylistBrowser.GetInsertIndex(playlist);
            return this.AddStationsToPlaylist(playlist, index, stations);
        }

        public async Task AddStationsToPlaylist(Playlist playlist, int index, IEnumerable<Station> stations)
        {
            using (var task = new AddStationsToPlaylistTask(playlist, index, stations, false))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public class RadioBrowserReport : ReportComponent
        {
            public RadioBrowserReport(RadioBrowserBehaviour behaviour, IEnumerable<Station> stations)
            {
                this.Behaviour = behaviour;
                this.Stations = stations;
            }

            public RadioBrowserBehaviour Behaviour { get; private set; }

            public IEnumerable<Station> Stations { get; private set; }

            public override string Title
            {
                get
                {
                    return Strings.RadioBrowserBehaviour_Path;
                }
            }

            public override string Description
            {
                get
                {
                    return string.Empty;
                }
            }

            public override string[] Headers
            {
                get
                {
                    return new[]
                    {
                        nameof(Station.Name),
                        nameof(Station.Url)
                    };
                }
            }

            public override IEnumerable<IReportComponentRow> Rows
            {
                get
                {
                    foreach (var station in this.Stations)
                    {
                        yield return new RadioBrowserReportRow(this, station);
                    }
                }
            }

            public class RadioBrowserReportRow : ReportComponentRow
            {
                public RadioBrowserReportRow(RadioBrowserReport report, Station station)
                {
                    this.Report = report;
                    this.Station = station;
                }

                public RadioBrowserReport Report { get; private set; }

                public Station Station { get; private set; }

                public override string[] Values
                {
                    get
                    {
                        return new[]
                        {
                            this.Station.Name,
                            this.Station.Url
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
                            return this.Report.Behaviour.AddStationsToPlaylist(new[] { this.Station });
                    }
                    return base.InvokeAsync(component);
                }
            }
        }


        private class AddStationsToPlaylistTask : PlaylistTaskBase
        {
            public AddStationsToPlaylistTask(Playlist playlist, int sequence, IEnumerable<Station> stations, bool clear) : base(playlist, sequence)
            {
                this.Stations = stations;
                this.Clear = clear;
            }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public IEnumerable<Station> Stations { get; private set; }

            public bool Clear { get; private set; }

            public IPlaylistBrowser PlaylistBrowser { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.PlaylistBrowser = core.Components.PlaylistBrowser;
                base.InitializeComponent(core);
            }

            protected override Task OnStarted()
            {
                this.Name = "Opening archive";
                return base.OnStarted();
            }

            protected override async Task OnRun()
            {
                if (this.Clear)
                {
                    await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
                }
                await this.AddStations(this.Stations, false).ConfigureAwait(false);
            }

            protected virtual async Task<int> AddStations(IEnumerable<Station> stations, bool maintainOrder)
            {
                var count = default(int);
                using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                {
                    count = await this.AddPlaylistItems(stations, cancellationToken).ConfigureAwait(false);
                    await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
                    await this.SequenceItems(maintainOrder).ConfigureAwait(false);
                    await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
                return count;
            }

            protected virtual async Task<int> AddPlaylistItems(IEnumerable<Station> stations, CancellationToken cancellationToken)
            {
                var count = default(int);
                var playlistItems = stations.Select(
                    station => new PlaylistItem()
                    {
                        DirectoryName = station.Url,
                        FileName = station.Url
                    }
                );
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<PlaylistItem>(transaction);
                    foreach (var playlistItem in playlistItems)
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", playlistItem.FileName);
                        playlistItem.Playlist_Id = this.Playlist.Id;
                        playlistItem.Sequence = this.Sequence;
                        playlistItem.Status = PlaylistItemStatus.Import;
                        await set.AddAsync(playlistItem).ConfigureAwait(false);
                        count++;
                        this.Offset++;
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
                return count;
            }

            protected override Task OnCompleted()
            {
                return this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated)));
            }
        }
    }
}
