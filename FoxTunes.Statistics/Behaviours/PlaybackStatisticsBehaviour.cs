using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class PlaybackStatisticsBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public StatisticsManager StatisticsManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStream OutputStream { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public SelectionConfigurationElement Trigger { get; private set; }

        public SelectionConfigurationElement Write { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.StatisticsManager = ComponentRegistry.Instance.GetComponent<StatisticsManager>();
            this.Output = core.Components.Output;
            this.PlaybackManager = core.Managers.Playback;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                PlaybackBehaviourConfiguration.SECTION,
                PlaybackStatisticsBehaviourConfiguration.ENABLED
            );
            this.Trigger = this.Configuration.GetElement<SelectionConfigurationElement>(
                PlaybackBehaviourConfiguration.SECTION,
                PlaybackStatisticsBehaviourConfiguration.TRIGGER
            );
            this.Enabled.ValueChanged += this.OnValueChanged;
            this.Write = this.Configuration.GetElement<SelectionConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.WRITE_ELEMENT
            );
            this.Refresh();
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        protected virtual void Refresh()
        {
            if (this.Enabled.Value)
            {
                this.Enable();
            }
            else
            {
                this.Disable();
            }
        }

        public void Enable()
        {
            if (this.Output != null)
            {
                this.Output.IsStartedChanged += this.OnIsStartedChanged;
            }
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            }
        }

        public void Disable()
        {
            if (this.Output != null)
            {
                this.Output.IsStartedChanged -= this.OnIsStartedChanged;
            }
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
        }

        protected virtual void OnIsStartedChanged(object sender, EventArgs e)
        {
            this.OutputStream = null;
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(() => this.Refresh(this.PlaybackManager.CurrentStream));
        }

        protected virtual Task Refresh(IOutputStream currentStream)
        {
            switch (PlaybackStatisticsBehaviourConfiguration.GetTrigger(this.Trigger.Value))
            {
                default:
                case UpdatePlaybackStatisticsTrigger.Begin:
                    return this.IncrementPlayCount(currentStream);
                case UpdatePlaybackStatisticsTrigger.End:
                    var task = default(Task);
                    if (this.OutputStream != null && this.OutputStream.IsEnded)
                    {
                        task = this.IncrementPlayCount(this.OutputStream);
                    }
                    else
                    {
#if NET40
                        task = TaskEx.FromResult(false);
#else
                        task = Task.CompletedTask;
#endif
                    }
                    this.OutputStream = currentStream;
                    return task;
            }

        }

        protected virtual Task IncrementPlayCount(IOutputStream currentStream)
        {
            if (currentStream == null || currentStream.PlaylistItem == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.IncrementPlayCount(currentStream.PlaylistItem);
        }

        protected virtual async Task IncrementPlayCount(PlaylistItem playlistItem)
        {
            try
            {
                await this.StatisticsManager.IncrementPlayCount(playlistItem).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to update play count for file \"{0}\": {1}", playlistItem.FileName, e.Message);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return PlaybackStatisticsBehaviourConfiguration.GetConfigurationSections();
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
            this.Disable();
            if (this.Enabled != null)
            {
                this.Enabled.ValueChanged -= this.OnValueChanged;
            }
            this.Disable();
        }

        ~PlaybackStatisticsBehaviour()
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
