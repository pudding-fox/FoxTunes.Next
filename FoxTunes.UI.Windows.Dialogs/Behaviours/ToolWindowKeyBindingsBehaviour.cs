using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ToolWindowKeyBindingsBehaviour : KeyBindingsBehaviourBase
    {
        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Manage { get; private set; }

        public ICommand ManageCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(ToolWindowBehaviour.Instance.Manage);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Manage = this.Configuration.GetElement<TextConfigurationElement>(
                ToolWindowKeyBindingsBehaviourConfiguration.SECTION,
                ToolWindowKeyBindingsBehaviourConfiguration.MANAGE_ELEMENT
            );
            if (this.Manage != null)
            {
                this.Commands.Add(this.Manage, this.ManageCommand);
                this.Manage.ValueChanged += this.OnValueChanged;
            }
            base.InitializeComponent(core);
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ToolWindowKeyBindingsBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
