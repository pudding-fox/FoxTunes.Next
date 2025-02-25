using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LayoutDesignerKeyBindingsBehaviour : KeyBindingsBehaviourBase
    {
        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement Edit { get; private set; }

        public ICommand EditCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(() =>
                    LayoutDesignerBehaviour.Instance.IsDesigning = !LayoutDesignerBehaviour.Instance.IsDesigning
                );
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Edit = this.Configuration.GetElement<TextConfigurationElement>(
                LayoutDesignerKeyBindingsBehaviourConfiguration.SECTION,
                LayoutDesignerKeyBindingsBehaviourConfiguration.EDIT_ELEMENT
            );
            if (this.Edit != null)
            {
                this.Commands.Add(this.Edit, this.EditCommand);
                this.Edit.ValueChanged += this.OnValueChanged;
            }
            base.InitializeComponent(core);
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LayoutDesignerKeyBindingsBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
