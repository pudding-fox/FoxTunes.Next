using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class TransparencyProviderBehaviour : StandardBehaviour
    {
        public bool Warned { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER
            ).ValueChanged += this.OnValueChanged;
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            if (!this.Warned)
            {
                this.UserInterface.Restart();
                this.Warned = true;
            }
        }
    }
}
