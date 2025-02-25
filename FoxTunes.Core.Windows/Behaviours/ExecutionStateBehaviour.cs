using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ExecutionStateBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const int UPDATE_INTERVAL = 10000;

        private Timer Timer { get; set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Sleep { get; private set; }

        public BooleanConfigurationElement OnlyWhilePlaying { get; private set; }

        public EXECUTION_STATE ExecutionState { get; private set; }

        protected virtual void Enable()
        {
            if (this.Timer == null)
            {
                this.Timer = new Timer();
                this.Timer.Interval = UPDATE_INTERVAL;
                this.Timer.Elapsed += this.OnElapsed;
                this.Timer.Start();
                Logger.Write(this, LogLevel.Debug, "Power state manager was started.");
            }
        }

        protected virtual void Disable()
        {
            if (this.Timer != null)
            {
                this.Timer.Stop();
                this.Timer.Elapsed -= this.OnElapsed;
                this.Timer.Dispose();
                this.Timer = null;
                Logger.Write(this, LogLevel.Debug, "Power state manager was stopped.");
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.Configuration = core.Components.Configuration;
            this.Sleep = this.Configuration.GetElement<SelectionConfigurationElement>(
                ExecutionStateBehaviourConfiguration.SECTION,
                ExecutionStateBehaviourConfiguration.SLEEP_ELEMENT
            );
            this.OnlyWhilePlaying = this.Configuration.GetElement<BooleanConfigurationElement>(
                ExecutionStateBehaviourConfiguration.SECTION,
                ExecutionStateBehaviourConfiguration.ONLY_WHILE_PLAYING_ELEMENT
            );
            this.Sleep.ConnectValue(value =>
            {
                this.ExecutionState = ExecutionStateBehaviourConfiguration.GetExecutionState(this.Sleep.Value);
                if (!this.SetThreadExecutionState())
                {
                    //If we can't set execution state then don't bother starting the timer.
                }
                if (this.ExecutionState.HasFlag(EXECUTION_STATE.ES_SYSTEM_REQUIRED) || this.ExecutionState.HasFlag(EXECUTION_STATE.ES_DISPLAY_REQUIRED))
                {
                    this.Enable();
                }
                else
                {
                    this.Disable();
                }
            });
            base.InitializeComponent(core);
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!this.SetThreadExecutionState())
                {
                    //If we can't set execution state then disable the timer.
                    this.Disable();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        protected virtual bool SetThreadExecutionState()
        {
            try
            {
                var preventSleep = true;
                if (this.OnlyWhilePlaying.Value)
                {
                    preventSleep = this.PlaybackManager != null && this.PlaybackManager.CurrentStream != null && this.PlaybackManager.CurrentStream.IsPlaying;
                }
                if (preventSleep)
                {
                    SetThreadExecutionState(this.ExecutionState);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to set thread execution state: {0}", e.Message);
                return false;
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ExecutionStateBehaviourConfiguration.GetConfigurationSections();
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
        }

        ~ExecutionStateBehaviour()
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE flags);
    }
}
