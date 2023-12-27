using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class BassOutputDeviceSelector : OutputDeviceSelector
    {
        public IUserInterface UserInterface { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public override Task ShowSettings()
        {
            return this.UserInterface.ShowSettings(
                Strings.BassOutputConfiguration_Section,
                new[]
                {
                    BassOutputConfiguration.SECTION
                }
            );
        }
    }
}
