using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Stop : ViewModelBase
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            base.InitializeComponent(core);
        }

        public ICommand Command
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                    () => this.PlaybackManager.Stop()
                );
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Stop();
        }
    }
}
