using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class DefaultKeyBindingsBehaviour : KeyBindingsBehaviourBase
    {
        public const string SEARCH = "5125ACDE-CC68-4DFE-82B0-F96A0ED303B6";

        public global::FoxTunes.ViewModel.Playback _Playback { get; private set; }

        public global::FoxTunes.ViewModel.Settings _Settings { get; private set; }

        public global::FoxTunes.ViewModel.Equalizer _Equalizer { get; private set; }

        public global::FoxTunes.ViewModel.FullScreen _FullScreen { get; private set; }

        public global::FoxTunes.ViewModel.PlaylistManager _PlaylistManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Play { get; private set; }

        public TextConfigurationElement Previous { get; private set; }

        public TextConfigurationElement Next { get; private set; }

        public TextConfigurationElement Stop { get; private set; }

        public TextConfigurationElement Settings { get; private set; }

        public TextConfigurationElement Search { get; private set; }

        public TextConfigurationElement Equalizer { get; private set; }

        public TextConfigurationElement FullScreen { get; private set; }

        public TextConfigurationElement PlaylistManager { get; private set; }

        public ICommand SearchCommand
        {
            get
            {
                //TODO: This is a hack. The SearchBox control listens for this signal. Nothing else uses this pattern.
                return CommandFactory.Instance.CreateCommand(
                    () => this.SignalEmitter.Send(new Signal(this, CommonSignals.PluginInvocation, new PluginInvocationSignalState(SEARCH)))
                );
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this._Playback = new global::FoxTunes.ViewModel.Playback(false);
            this._Settings = new global::FoxTunes.ViewModel.Settings();
            this._Equalizer = new global::FoxTunes.ViewModel.Equalizer();
            this._FullScreen = new global::FoxTunes.ViewModel.FullScreen();
            this._PlaylistManager = new global::FoxTunes.ViewModel.PlaylistManager();
            this.SignalEmitter = core.Components.SignalEmitter;
            this.Configuration = core.Components.Configuration;
            this.Play = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.PLAY_ELEMENT
            );
            this.Previous = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.PREVIOUS_ELEMENT
            );
            this.Next = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.NEXT_ELEMENT
            );
            this.Stop = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.STOP_ELEMENT
            );
            this.Settings = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.SETTINGS_ELEMENT
            );
            this.Search = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.SEARCH_ELEMENT
            );
            this.Equalizer = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.EQUALIZER_ELEMENT
            );
            this.FullScreen = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.FULL_SCREEN_ELEMENT
            );
            this.PlaylistManager = this.Configuration.GetElement<TextConfigurationElement>(
                DefaultKeyBindingsBehaviourConfiguration.SECTION,
                DefaultKeyBindingsBehaviourConfiguration.PLAYLIST_MANAGER
            );
            if (this.Play != null)
            {
                this.Commands.Add(this.Play, this._Playback.PlayCommand);
                this.Play.ValueChanged += this.OnValueChanged;
            }
            if (this.Next != null)
            {
                this.Commands.Add(this.Next, this._Playback.NextCommand);
                this.Next.ValueChanged += this.OnValueChanged;
            }
            if (this.Previous != null)
            {
                this.Commands.Add(this.Previous, this._Playback.PreviousCommand);
                this.Previous.ValueChanged += this.OnValueChanged;
            }
            if (this.Stop != null)
            {
                this.Commands.Add(this.Stop, this._Playback.StopOutputCommand);
                this.Stop.ValueChanged += this.OnValueChanged;
            }
            if (this.Settings != null)
            {
                this.Commands.Add(this.Settings, this._Settings.WindowState.ShowCommand);
                this.Settings.ValueChanged += this.OnValueChanged;
            }
            if (this.Search != null)
            {
                this.Commands.Add(this.Search, this.SearchCommand);
                this.Search.ValueChanged += this.OnValueChanged;
            }
            if (this.Equalizer != null)
            {
                this.Commands.Add(this.Equalizer, this._Equalizer.WindowState.ShowCommand);
                this.Equalizer.ValueChanged += this.OnValueChanged;
            }
            if (this.FullScreen != null)
            {
                this.Commands.Add(this.FullScreen, this._FullScreen.ToggleCommand);
                this.FullScreen.ValueChanged += this.OnValueChanged;
            }
            if (this.PlaylistManager != null)
            {
                this.Commands.Add(this.PlaylistManager, this._PlaylistManager.WindowState.ShowCommand);
                this.PlaylistManager.ValueChanged += this.OnValueChanged;
            }
            base.InitializeComponent(core);
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return DefaultKeyBindingsBehaviourConfiguration.GetConfigurationSections();
        }

        protected override void Dispose(bool disposing)
        {
            if (this._Playback != null)
            {
                this._Playback.Dispose();
            }
            if (this._Settings != null)
            {
                this._Settings.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
