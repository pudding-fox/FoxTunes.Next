using Discord;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class DiscordBehaviour : StandardBehaviour, IConfigurableComponent
    {
        static DiscordBehaviour()
        {
            Loader.Load("discord_game_sdk.dll");
        }

        public const long CLIENT_ID = 1357689312660946984;

        public IPlaybackManager PlaybackManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.Configuration = core.Components.Configuration;
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

        public Discord.Discord Discord { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            this.Discord = new global::Discord.Discord(CLIENT_ID, Convert.ToUInt64(global::Discord.CreateFlags.Default));
            this.Discord.SetLogHook(global::Discord.LogLevel.Debug, this.OnLog);
            this.Discord.GetApplicationManager().GetOAuth2Token(this.OnGetOAuth2Token);
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
            this.Enabled = false;
        }

        public void Refresh()
        {
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
            var activity = new global::Discord.Activity()
            {
                Type = global::Discord.ActivityType.Listening,
                State = "Playing",
                Details = "A song"
            };
            this.Discord.GetActivityManager().UpdateActivity(activity, this.OnUpdateActivity);
        }

        protected virtual void ClearActivity()
        {
            this.Discord.ActivityManagerInstance.ClearActivity(this.OnClearActivity);
        }

        protected virtual void OnLog(global::Discord.LogLevel level, string message)
        {
            //Nothing to do.
        }

        protected virtual void OnGetOAuth2Token(Result result, ref OAuth2Token token)
        {
            //Nothing to do.
        }

        protected virtual void OnUpdateActivity(Result result)
        {
            //Nothing to do.
        }

        protected virtual void OnClearActivity(Result result)
        {
            //Nothing to do.
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
    }
}
