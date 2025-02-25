using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using ManagedBass.Cd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    public class BassCdBehaviour : StandardBehaviour, IConfigurableComponent, IInvocableComponent, IDisposable
    {
        public const string OPEN_CD = "FFFF";

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassCdBehaviour).Assembly.Location);
            }
        }

        public BassCdBehaviour()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_gapless_cd.dll"));
        }

        public ICore Core { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IBassOutput Output { get; private set; }

        public CdDoorMonitor DoorMonitor { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

        private bool _Enabled { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                Logger.Write(this, LogLevel.Debug, "Enabled = {0}", this.Enabled);
            }
        }

        private bool _CdLookup { get; set; }

        public bool CdLookup
        {
            get
            {
                return this._CdLookup;
            }
            private set
            {
                this._CdLookup = value;
                Logger.Write(this, LogLevel.Debug, "CD Lookup = {0}", this.CdLookup);
            }
        }

        private IEnumerable<string> _CdLookupHosts { get; set; }

        public IEnumerable<string> CdLookupHosts
        {
            get
            {
                return this._CdLookupHosts;
            }
            private set
            {
                this._CdLookupHosts = value;
                if (value != null)
                {
                    foreach (var cdLookupHost in value)
                    {
                        Logger.Write(this, LogLevel.Debug, "CD Lookup Host = {0}", cdLookupHost);
                    }
                }
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Output = core.Components.Output as IBassOutput;
            this.PlaylistManager = core.Managers.Playlist;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.DoorMonitor = ComponentRegistry.Instance.GetComponent<CdDoorMonitor>();
            if (this.DoorMonitor != null)
            {
                this.DoorMonitor.StateChanged += this.OnStateChanged;
            }
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassCdStreamProviderBehaviourConfiguration.SECTION,
                BassCdStreamProviderBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => this.Enabled = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassCdStreamProviderBehaviourConfiguration.SECTION,
                BassCdStreamProviderBehaviourConfiguration.LOOKUP_ELEMENT
            ).ConnectValue(value => this.CdLookup = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                BassCdStreamProviderBehaviourConfiguration.SECTION,
                BassCdStreamProviderBehaviourConfiguration.LOOKUP_HOST_ELEMENT
            ).ConnectValue(value =>
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.CdLookupHosts = value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    this.CdLookupHosts = BassCdStreamProviderBehaviourConfiguration.GetLookupHosts();
                }
            });
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline += this.OnCreatingPipeline;
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnCreatingPipeline(object sender, CreatingPipelineEventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }
            var drive = default(int);
            var id = default(string);
            var track = default(int);
            if (!CdUtils.ParseUrl(e.Stream.FileName, out drive, out id, out track))
            {
                return;
            }
            if (e.Input != null)
            {
                Logger.Write(this, LogLevel.Debug, "Overriding the default pipeline input: {0}", e.Input.GetType().Name);
                e.Input.Dispose();
            }
            e.Input = new BassCdStreamInput(this, drive, e.Pipeline, e.Stream.Flags);
            e.Input.InitializeComponent(this.Core);
        }

        protected virtual void OnStateChanged(object sender, EventArgs e)
        {
            if (this.Output.IsStarted && this.DoorMonitor.State == CdDoorState.Open)
            {
                Logger.Write(this, LogLevel.Debug, "CD door was opened, shutting down the output.");
                this.Output.Shutdown();
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassCdStreamProviderBehaviourConfiguration.GetConfigurationSections();
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
                if (this.Enabled)
                {
                    foreach (var drive in CdUtils.GetDrives())
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, OPEN_CD, drive, path: Strings.BassCdStreamProviderBehaviour_Path);
                    }
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case OPEN_CD:
                    var drive = CdUtils.GetDrive(component.Name);
                    if (drive == CdUtils.NO_DRIVE)
                    {
                        break;
                    }
                    return this.OpenCd(drive);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task OpenCd(int drive)
        {
            return this.OpenCd(this.PlaylistManager.SelectedPlaylist, drive);
        }

        public async Task OpenCd(Playlist playlist, int drive)
        {
            using (var task = new AddCdToPlaylistTask(playlist, drive, this.CdLookup, this.CdLookupHosts))
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
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline -= this.OnCreatingPipeline;
            }
        }

        ~BassCdBehaviour()
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

        private class AddCdToPlaylistTask : PlaylistTaskBase
        {
            public AddCdToPlaylistTask(Playlist playlist, int drive, bool cdLookup, IEnumerable<string> cdLookupHosts) : base(playlist)
            {
                this.Drive = drive;
                this.CdLookup = cdLookup;
                this.CdLookupHosts = cdLookupHosts;
            }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public int Drive { get; private set; }

            public bool CdLookup { get; private set; }

            public IEnumerable<string> CdLookupHosts { get; private set; }

            public IPlaylistManager PlaylistManager { get; private set; }

            public IPlaylistBrowser PlaylistBrowser { get; private set; }

            public CdPlaylistItemFactory Factory { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.PlaylistManager = core.Managers.Playlist;
                this.PlaylistBrowser = core.Components.PlaylistBrowser;
                this.Factory = new CdPlaylistItemFactory(this.Drive, this.CdLookup, this.CdLookupHosts, this.Visible);
                this.Factory.InitializeComponent(core);
                base.InitializeComponent(core);
            }

            protected override async Task OnRun()
            {
                this.Name = "Opening CD";
                try
                {
                    if (!BassCd.IsReady(this.Drive))
                    {
                        throw new InvalidOperationException("Drive is not ready.");
                    }
                    var playlist = this.PlaylistManager.SelectedPlaylist;
                    var playlistItems = default(PlaylistItem[]);
                    await this.WithSubTask(this.Factory, async () => playlistItems = await this.Factory.Create(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
                    using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                    {
                        //Always append for now.
                        this.Sequence = this.PlaylistBrowser.GetInsertIndex(this.PlaylistManager.SelectedPlaylist);
                        await this.AddPlaylistItems(playlist, playlistItems).ConfigureAwait(false);
                        await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
                        await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
                    }))
                    {
                        await task.Run().ConfigureAwait(false);
                    }
                }
                finally
                {
                    //Ignoring result on purpose.
                    BassCd.Release(this.Drive);
                }
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
            }

            private async Task AddPlaylistItems(Playlist playlist, IEnumerable<PlaylistItem> playlistItems)
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<PlaylistItem>(transaction);
                    var position = 0;
                    foreach (var playlistItem in playlistItems)
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", playlistItem.FileName);
                        playlistItem.Playlist_Id = playlist.Id;
                        playlistItem.Sequence = this.Sequence + position;
                        playlistItem.Status = PlaylistItemStatus.Import;
                        await set.AddAsync(playlistItem).ConfigureAwait(false);
                        position++;
                    }
                    this.Offset += position;
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }
        }
    }
}