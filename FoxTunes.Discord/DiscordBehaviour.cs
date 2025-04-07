using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public class DiscordBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public static readonly int INTERVAL = 1000;

        static DiscordBehaviour()
        {
            Loader.Load("discord_game_sdk.dll");
        }

        public const long CLIENT_ID = 1357689312660946984;

        public IPlaybackManager PlaybackManager { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public IScriptingContext ScriptingContext { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private string _StateScript { get; set; }

        public string StateScript
        {
            get
            {
                return this._StateScript;
            }
            set
            {
                this._StateScript = value;
                this.OnStateScriptChanged();
            }
        }

        protected virtual void OnStateScriptChanged()
        {
            this.Refresh();
            if (this.StateScriptChanged != null)
            {
                this.StateScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StateScript");
        }

        public event EventHandler StateScriptChanged;

        private string _DetailsScript { get; set; }

        public string DetailsScript
        {
            get
            {
                return this._DetailsScript;
            }
            set
            {
                this._DetailsScript = value;
                this.OnDetailsScriptChanged();
            }
        }

        protected virtual void OnDetailsScriptChanged()
        {
            this.Refresh();
            if (this.DetailsScriptChanged != null)
            {
                this.DetailsScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DetailsScript");
        }

        public event EventHandler DetailsScriptChanged;

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.ScriptingContext = this.ScriptingRuntime.CreateContext();
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.STATE_SCRIPT
            ).ConnectValue(value => this.StateScript = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.DETAILS_SCRIPT
            ).ConnectValue(value => this.DetailsScript = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.ENABLED
            ).ConnectValue(value =>
            {
                if (value)
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

        public bool Enabled { get; private set; }

        public global::Discord.Discord Discord { get; private set; }

        public global::System.Timers.Timer Timer { get; private set; }

        public string Token { get; private set; }

        public string Scopes { get; private set; }

        public long Expires { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            this.Discord = new global::Discord.Discord(CLIENT_ID, Convert.ToUInt64(global::Discord.CreateFlags.Default));
            this.Discord.SetLogHook(global::Discord.LogLevel.Debug, (level, message) =>
            {
                switch (level)
                {
                    case global::Discord.LogLevel.Debug:
                        Logger.Write(this, LogLevel.Debug, message);
                        break;
                    case global::Discord.LogLevel.Info:
                        Logger.Write(this, LogLevel.Info, message);
                        break;
                    case global::Discord.LogLevel.Warn:
                        Logger.Write(this, LogLevel.Warn, message);
                        break;
                    case global::Discord.LogLevel.Error:
                        Logger.Write(this, LogLevel.Error, message);
                        break;
                }
            });
            this.Discord.RunCallbacks();
            this.Discord.GetApplicationManager().GetOAuth2Token((global::Discord.Result result, ref global::Discord.OAuth2Token token) =>
            {
                switch (result)
                {
                    case global::Discord.Result.Ok:
                        this.Token = token.AccessToken;
                        this.Scopes = token.Scopes;
                        this.Expires = token.Expires;
                        break;
                    default:
                        Logger.Write(this, LogLevel.Error, "Failed to get token: {0}", Enum.GetName(typeof(global::Discord.Result), result));
                        break;
                }
            });
            this.Discord.RunCallbacks();
            this.Timer = new global::System.Timers.Timer();
            this.Timer.Interval = INTERVAL;
            this.Timer.AutoReset = false;
            this.Timer.Elapsed += this.OnElapsed;
            this.Timer.Start();
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.Enabled = true;
        }

        public void Disable()
        {
            if (!this.Enabled)
            {
                return;
            }
            this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            if (this.Discord != null)
            {
                this.Discord.Dispose();
                this.Discord = null;
            }
            if (this.Timer != null)
            {
                this.Timer.Stop();
                this.Timer.Elapsed -= this.OnElapsed;
                this.Timer.Dispose();
                this.Timer = null;
            }
            this.Enabled = false;
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this.Discord != null)
                {
                    this.Discord.RunCallbacks();
                }
                if (this.Timer != null)
                {
                    this.Timer.Start();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        public void Refresh()
        {
            if (this.Discord == null)
            {
                return;
            }
            if (this.PlaybackManager.CurrentStream != null)
            {
                this.UpdateActivity(this.PlaybackManager.CurrentStream);
            }
            else
            {
                this.ClearActivity();
            }
        }

        protected virtual void UpdateActivity(IOutputStream outputStream)
        {
            var activity = this.GetActivity(outputStream);
            this.Discord.GetActivityManager().UpdateActivity(activity, result =>
            {
                //Nothing to do.
            });
            this.Discord.RunCallbacks();
        }

        protected virtual global::Discord.Activity GetActivity(IOutputStream outputStream)
        {
            return new global::Discord.Activity()
            {
                State = this.GetState(outputStream),
                Details = this.GetDetails(outputStream),
            };
        }

        protected virtual string GetState(IOutputStream outputStream)
        {
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.StateScript
            );
            runner.Prepare();
            return Convert.ToString(runner.Run());
        }

        protected virtual string GetDetails(IOutputStream outputStream)
        {
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.DetailsScript
            );
            runner.Prepare();
            return Convert.ToString(runner.Run());
        }

        protected virtual void ClearActivity()
        {
            this.Discord.ActivityManagerInstance.ClearActivity(result =>
            {
                //Nothing to do.
            });
            this.Discord.RunCallbacks();
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return DiscordBehaviourConfiguration.GetConfigurationSections();
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

        ~DiscordBehaviour()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
