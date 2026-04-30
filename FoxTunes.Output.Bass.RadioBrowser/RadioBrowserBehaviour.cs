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
    }
}
