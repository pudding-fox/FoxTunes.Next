using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class MiniPlayerkeyBindingsBehaviour : KeyBindingsBehaviourBase
    {
        public global::FoxTunes.ViewModel.MiniPlayer _MiniPlayer { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Toggle { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this._MiniPlayer = new global::FoxTunes.ViewModel.MiniPlayer();
            this.Configuration = core.Components.Configuration;
            this.Toggle = this.Configuration.GetElement<TextConfigurationElement>(
                MiniPlayerKeyBindingsBehaviourConfiguration.SECTION,
                MiniPlayerKeyBindingsBehaviourConfiguration.TOGGLE_ELEMENT
            );
            if (this.Toggle != null)
            {
                this.Commands.Add(this.Toggle, this._MiniPlayer.ToggleCommand);
                this.Toggle.ValueChanged += this.OnValueChanged;
            }
            base.InitializeComponent(core);
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return MiniPlayerKeyBindingsBehaviourConfiguration.GetConfigurationSections();
        }

        protected override void Dispose(bool disposing)
        {
            if (this._MiniPlayer != null)
            {
                this._MiniPlayer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
