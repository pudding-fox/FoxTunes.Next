using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using RadioBrowserWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddStationsToPlaylistTask : PlaylistTaskBase
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
            this.Name = "Scanning";
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
            var playlistItems = this.CreatePlaylistItems(stations);
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

        protected virtual IEnumerable<PlaylistItem> CreatePlaylistItems(IEnumerable<Station> stations)
        {
            foreach (var station in stations)
            {
                var m3uSuffixes = new[]
                {
                    "m3u",
                    "m3u?",
                    "m3u8",
                    "m3u8?"
                };
                if (m3uSuffixes.Any(m3uSuffix => station.UrlResolved.EndsWith(m3uSuffix, StringComparison.OrdinalIgnoreCase)))
                {
                    var helper = M3UHelper.FromUrl(station.UrlResolved);
                    foreach (var playlistItem in helper.Read())
                    {
                        yield return playlistItem;
                    }
                }
                else
                {
                    yield return new PlaylistItem()
                    {
                        DirectoryName = station.UrlResolved,
                        FileName = station.UrlResolved
                    };
                }
            }
        }

        protected override Task OnCompleted()
        {
            return this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated)));
        }
    }
}
