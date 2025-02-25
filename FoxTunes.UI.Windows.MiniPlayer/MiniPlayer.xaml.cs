namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for MiniPlayer.xaml
    /// </summary>
    [UIComponent("3EAA32EE-9CB2-491B-928E-EA1E9E547E30", role: UIComponentRole.Launcher)]
    [UIComponentToolbar(1200, UIComponentToolbarAlignment.Right, true)]
    public partial class MiniPlayer : UIComponentBase
    {
        public MiniPlayer()
        {
            this.InitializeComponent();
        }
    }
}
