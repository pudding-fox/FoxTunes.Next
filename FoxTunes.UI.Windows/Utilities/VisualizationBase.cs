using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;

namespace FoxTunes
{
    public abstract class VisualizationBase : RendererBase
    {
        public readonly object SyncRoot = new object();

        public bool Enabled { get; private set; }

        private int _UpdateInterval { get; set; }

        public int UpdateInterval
        {
            get
            {
                return this._UpdateInterval;
            }
            protected set
            {
                this._UpdateInterval = value;
                this.OnUpdateIntervalChanged();
            }
        }

        protected virtual void OnUpdateIntervalChanged()
        {
            lock (this.SyncRoot)
            {
                if (this.Timer1 != null)
                {
                    this.Timer1.Interval = this.UpdateInterval;
                }
                if (this.Timer2 != null)
                {
                    this.Timer2.Interval = this.UpdateInterval;
                }
            }
        }

        protected global::System.Timers.Timer Timer1 { get; private set; }

        protected global::System.Timers.Timer Timer2 { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Timer1 = new global::System.Timers.Timer();
            this.Timer1.AutoReset = false;
            this.Timer1.Elapsed += this.OnUpdateData;
            this.Timer2 = new global::System.Timers.Timer();
            this.Timer2.AutoReset = false;
            this.Timer2.Elapsed += this.OnUpdateDisplay;
            PlaybackStateNotifier.IsStartedChanged += this.OnIsStartedChanged;
            PlaybackStateNotifier.IsPlayingChanged += this.OnIsPlayingChanged;
            base.InitializeComponent(core);
            this.Update();
        }

        protected virtual void OnIsStartedChanged(object sender, EventArgs e)
        {
            this.Update();
        }

        protected virtual void OnIsPlayingChanged(object sender, EventArgs e)
        {
            this.Update();
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<IntegerConfigurationElement>(
                   VisualizationBehaviourConfiguration.SECTION,
                   VisualizationBehaviourConfiguration.INTERVAL_ELEMENT
                ).ConnectValue(value => this.UpdateInterval = value);
            }
            base.OnConfigurationChanged();
        }

        protected virtual void Update()
        {
            if (!PlaybackStateNotifier.IsStarted)
            {
                var task = this.Clear();
            }
            if (PlaybackStateNotifier.IsPlaying && !this.Enabled)
            {
                Logger.Write(this, LogLevel.Debug, "Playback was started, starting renderer.");
                this.Start();
            }
            else if (!PlaybackStateNotifier.IsPlaying && this.Enabled)
            {
                Logger.Write(this, LogLevel.Debug, "Playback was stopped, stopping renderer.");
                this.Stop();
            }
        }

        public void Start()
        {
            lock (this.SyncRoot)
            {
                if (this.Timer1 != null)
                {
                    this.Timer1.Start();
                }
                if (this.Timer2 != null)
                {
                    this.Timer2.Start();
                }
                this.Enabled = true;
            }
        }

        protected virtual void BeginUpdateData()
        {
            lock (this.SyncRoot)
            {
                if (!this.Enabled)
                {
                    return;
                }
                if (this.Timer1 != null)
                {
                    this.Timer1.Start();
                }
            }
        }

        protected virtual void BeginUpdateDisplay()
        {
            lock (this.SyncRoot)
            {
                if (!this.Enabled)
                {
                    return;
                }
                if (this.Timer2 != null)
                {
                    this.Timer2.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this.SyncRoot)
            {
                if (this.Timer1 != null)
                {
                    this.Timer1.Stop();
                }
                if (this.Timer2 != null)
                {
                    this.Timer2.Stop();
                }
                this.Enabled = false;
            }
        }

        protected abstract void OnUpdateData(object sender, ElapsedEventArgs e);

        protected abstract void OnUpdateDisplay(object sender, ElapsedEventArgs e);

#if DEBUG

        protected global::FoxTunes.ViewModel.Visualization ViewModel;

        protected override async Task<bool> CreateBitmap()
        {
            var success = await base.CreateBitmap().ConfigureAwait(false);
            if (success)
            {
                await Windows.Invoke(() =>
                {
                    var visualization = this.FindAncestor<Visualization>();
                    if (visualization != null)
                    {
                        var grid = visualization.FindChild<Grid>();
                        if (grid != null)
                        {
                            grid.TryFindResource("ViewModel", out this.ViewModel);
                        }
                    }
                }).ConfigureAwait(false);
            }
            return success;
        }

#endif

        protected override void OnDisposing()
        {
            PlaybackStateNotifier.IsStartedChanged -= this.OnIsStartedChanged;
            PlaybackStateNotifier.IsPlayingChanged -= this.OnIsPlayingChanged;
            lock (this.SyncRoot)
            {
                if (this.Timer1 != null)
                {
                    this.Timer1.Elapsed -= this.OnUpdateData;
                    this.Timer1.Dispose();
                    this.Timer1 = null;
                }
                if (this.Timer2 != null)
                {
                    this.Timer2.Elapsed -= this.OnUpdateDisplay;
                    this.Timer2.Dispose();
                    this.Timer2 = null;
                }
            }
            base.OnDisposing();
        }
    }
}
