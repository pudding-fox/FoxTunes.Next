using FoxTunes.Interfaces;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Settings : ViewModelBase
    {
        public Settings()
        {
            this.WindowState = new WindowState(SettingsWindow.ID);
        }

        public WindowState WindowState { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Settings();
        }
    }
}
