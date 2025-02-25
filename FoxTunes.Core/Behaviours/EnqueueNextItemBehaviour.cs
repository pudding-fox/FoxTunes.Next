using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class EnqueueNextItemsBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const int TIMEOUT = 1000;

        public EnqueueNextItemsBehaviour()
        {
            this.Debouncer = new Debouncer(TIMEOUT);
        }

        public Debouncer Debouncer { get; private set; }

        public IOutput Output { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IOutputStreamQueue OutputStreamQueue { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Count { get; private set; }

        public BooleanConfigurationElement Wrap { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaybackManager = core.Managers.Playback;
            this.OutputStreamQueue = core.Components.OutputStreamQueue;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.Configuration = core.Components.Configuration;
            this.Count = this.Configuration.GetElement<IntegerConfigurationElement>(
                EnqueueNextItemBehaviourConfiguration.SECTION,
                EnqueueNextItemBehaviourConfiguration.COUNT
            );
            this.Wrap = this.Configuration.GetElement<BooleanConfigurationElement>(
                EnqueueNextItemBehaviourConfiguration.SECTION,
                EnqueueNextItemBehaviourConfiguration.WRAP
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Debouncer.Exec(() => this.Dispatch(this.EnqueueItems));
        }

        private async Task EnqueueItems()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return;
            }
            var playlistItem = outputStream.PlaylistItem;
            for (var position = 0; position < this.Count.Value; position++)
            {
                if (!this.Output.IsStarted)
                {
                    Logger.Write(this, LogLevel.Debug, "Output was stopped, cancelling.");
                    break;
                }
                playlistItem = this.PlaylistBrowser.GetNextItem(playlistItem, this.Wrap.Value);
                if (playlistItem == null)
                {
                    return;
                }
                if (this.OutputStreamQueue.Requeue(playlistItem))
                {
                    //TODO: What is this Delay for?
#if NET40
                    await TaskEx.Delay(1).ConfigureAwait(false);
#else
                    await Task.Delay(1).ConfigureAwait(false);
#endif
                    continue;
                }
                Logger.Write(this, LogLevel.Debug, "Preemptively buffering playlist item: {0} => {1}", playlistItem.Id, playlistItem.FileName);
                await this.PlaybackManager.Load(playlistItem, false).ConfigureAwait(false);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return EnqueueNextItemBehaviourConfiguration.GetConfigurationSections();
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
            if (this.Debouncer != null)
            {
                this.Debouncer.Dispose();
            }
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
        }

        ~EnqueueNextItemsBehaviour()
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
    }
}
