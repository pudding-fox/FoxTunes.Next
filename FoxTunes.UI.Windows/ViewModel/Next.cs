using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Next : ViewModelBase
    {
        public IPlaylistManager PlaylistManager { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            base.InitializeComponent(core);
        }

        public ICommand Command
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                    () => this.PlaylistManager.Next()
                );
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Next();
        }
    }
}
