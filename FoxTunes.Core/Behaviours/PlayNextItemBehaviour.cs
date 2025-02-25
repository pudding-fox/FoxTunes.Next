using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class PlayNextItemBehaviour : StandardBehaviour, IDisposable
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Wrap { get; private set; }


        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.Ended += this.OnEnded;
            this.PlaylistManager = core.Managers.Playlist;
            this.Configuration = core.Components.Configuration;
            this.Wrap = this.Configuration.GetElement<BooleanConfigurationElement>(
                EnqueueNextItemBehaviourConfiguration.SECTION,
                EnqueueNextItemBehaviourConfiguration.WRAP
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnEnded(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Stream was stopped likely due to reaching the end, playing next item.");
            this.Dispatch(() => this.PlaylistManager.Next(this.Wrap.Value));
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
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.Ended -= this.OnEnded;
            }
        }

        ~PlayNextItemBehaviour()
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
