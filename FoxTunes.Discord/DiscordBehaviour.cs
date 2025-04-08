using Discord.Net;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

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

        public IErrorEmitter ErrorEmitter { get; private set; }

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
            this.ErrorEmitter = core.Components.ErrorEmitter;
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

        public IntPtr Discord { get; private set; }

        public string AccessToken { get; private set; }

        public string Scopes { get; private set; }

        public long Expires { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            this.Discord = DiscordManager.Create(CLIENT_ID, DiscordManager.CreateFlags.Default);
            if (IntPtr.Zero.Equals(this.Discord))
            {
                this.ErrorEmitter.Send(this, "Failed to initialise the discord sdk.");
                return;
            }
            {
                var result = DiscordManager.GetResult(this.Discord);
                if (result != DiscordManager.Result.Ok)
                {
                    this.ErrorEmitter.Send(this, string.Format("Failed to initialise the discord sdk: {0}", Enum.GetName(typeof(DiscordManager.Result), result)));
                    this.Disable();
                    return;
                }
            }
            {
                DiscordManager.FetchToken(this.Discord);
                var result = DiscordManager.WaitForResult(this.Discord);
                if (result != DiscordManager.Result.Ok)
                {
                    this.ErrorEmitter.Send(this, string.Format("Failed to authenticate: {0}", Enum.GetName(typeof(DiscordManager.Result), result)));
                    this.Disable();
                    return;
                }
            }
            {
                var token = default(DiscordManager.OAuth2Token);
                DiscordManager.GetToken(this.Discord, ref token);
                this.AccessToken = token.AccessToken;
                this.Scopes = token.Scopes;
                this.Expires = token.Expires;

            }
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
            if (!IntPtr.Zero.Equals(this.Discord))
            {
                DiscordManager.Free(this.Discord);
                this.Discord = IntPtr.Zero;
            }
            this.Enabled = false;
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
            DiscordManager.SetActivity(this.Discord, ref activity);
            DiscordManager.UpdateActivity(this.Discord);
            var result = DiscordManager.WaitForResult(this.Discord);
            if (result != DiscordManager.Result.Ok)
            {
                this.ErrorEmitter.Send(this, string.Format("Failed to update activity: {0}", Enum.GetName(typeof(DiscordManager.Result), result)));
                this.Disable();
                return;
            }
        }

        protected virtual DiscordManager.Activity GetActivity(IOutputStream outputStream)
        {
            return new DiscordManager.Activity()
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
            DiscordManager.ClearActivity(this.Discord);
            var result = DiscordManager.WaitForResult(this.Discord);
            if (result != DiscordManager.Result.Ok)
            {
                this.ErrorEmitter.Send(this, string.Format("Failed to clear activity: {0}", Enum.GetName(typeof(DiscordManager.Result), result)));
                this.Disable();
                return;
            }
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
