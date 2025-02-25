using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class NowPlaying : ViewModelBase, IConfigurationTarget
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public IScriptingContext ScriptingContext { get; private set; }

        private IConfiguration _Configuration { get; set; }

        public IConfiguration Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<TextConfigurationElement>(
                    new[] { NowPlayingConfiguration.SECTION, MiniPlayerBehaviourConfiguration.SECTION },
                    new[] { NowPlayingConfiguration.NOW_PLAYING_SCRIPT_ELEMENT, MiniPlayerBehaviourConfiguration.NOW_PLAYING_SCRIPT_ELEMENT }
                ).ConnectValue(async value => await this.SetScript(value).ConfigureAwait(false));
                this.MarqueeInterval = this.Configuration.GetElement<IntegerConfigurationElement>(
                  MiniPlayerBehaviourConfiguration.SECTION,
                  MiniPlayerBehaviourConfiguration.MARQUEE_INTERVAL_ELEMENT
                );
                this.MarqueeStep = this.Configuration.GetElement<DoubleConfigurationElement>(
                  MiniPlayerBehaviourConfiguration.SECTION,
                  MiniPlayerBehaviourConfiguration.MARQUEE_STEP_ELEMENT
                );
                this.Dispatch(this.Refresh);
            }
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

        private IntegerConfigurationElement _MarqueeInterval { get; set; }

        public IntegerConfigurationElement MarqueeInterval
        {
            get
            {
                return this._MarqueeInterval;
            }
            set
            {
                this._MarqueeInterval = value;
                this.OnMarqueeIntervalChanged();
            }
        }

        protected virtual void OnMarqueeIntervalChanged()
        {
            if (this.MarqueeIntervalChanged != null)
            {
                this.MarqueeIntervalChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MarqueeInterval");
        }

        public event EventHandler MarqueeIntervalChanged;

        private DoubleConfigurationElement _MarqueeStep { get; set; }

        public DoubleConfigurationElement MarqueeStep
        {
            get
            {
                return this._MarqueeStep;
            }
            set
            {
                this._MarqueeStep = value;
                this.OnMarqueeStepChanged();
            }
        }

        protected virtual void OnMarqueeStepChanged()
        {
            if (this.MarqueeStepChanged != null)
            {
                this.MarqueeStepChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MarqueeStep");
        }

        public event EventHandler MarqueeStepChanged;

        private PlaylistItem _CurrentItem { get; set; }

        public PlaylistItem CurrentItem
        {
            get
            {
                return this._CurrentItem;
            }
            private set
            {
                this._CurrentItem = value;
                this.OnCurrentItemChanged();
            }
        }

        protected virtual void OnCurrentItemChanged()
        {
            if (this.CurrentItemChanged != null)
            {
                this.CurrentItemChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentItem");
        }

        public event EventHandler CurrentItemChanged;

        private string _Script { get; set; }

        public string Script
        {
            get
            {
                return this._Script;
            }
        }

        public Task SetScript(string value)
        {
            this._Script = value;
            return this.OnScriptChanged();
        }

        protected virtual async Task OnScriptChanged()
        {
            await this.Refresh().ConfigureAwait(false);
            if (this.ScriptChanged != null)
            {
                this.ScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Script");
        }

        public event EventHandler ScriptChanged;

        private object _Value { get; set; }

        public object Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        private bool _IsBuffering { get; set; }

        public bool IsBuffering
        {
            get
            {
                return this._IsBuffering;
            }
            set
            {
                this._IsBuffering = value;
                this.OnIsBufferingChanged();
            }
        }

        protected virtual void OnIsBufferingChanged()
        {
            if (this.IsBufferingChanged != null)
            {
                this.IsBufferingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsBuffering");
        }

        public event EventHandler IsBufferingChanged;

        protected override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.ScriptingContext = this.ScriptingRuntime.CreateContext();
            base.InitializeComponent(core);
        }

        protected virtual async void OnActiveChanged(object sender, EventArgs e)
        {
            //TODO: Might it be cleaner to expose this logic directly on the IsBuffering getter?
            var tasks = global::FoxTunes.BackgroundTask.Active
                .OfType<LoadOutputStreamTask>()
                .Where(task => task.Visible);
            if (tasks.Any())
            {
                await Windows.Invoke(() => this.IsBuffering = true).ConfigureAwait(false);
            }
            else if (this.IsBuffering)
            {
                await Windows.Invoke(() => this.IsBuffering = false).ConfigureAwait(false);
            }
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected virtual Task Refresh()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.Script
            );
            runner.Prepare();
            var value = runner.Run();
            return Windows.Invoke(() =>
            {
                if (outputStream != null)
                {
                    this.CurrentItem = outputStream.PlaylistItem;
                }
                else
                {
                    this.CurrentItem = null;
                }
                this.Value = value;
            });
        }

        protected override Freezable CreateInstanceCore()
        {
            return new NowPlaying();
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            if (this.ScriptingContext != null)
            {
                this.ScriptingContext.Dispose();
            }
            base.OnDisposing();
        }
    }
}
