namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for MoodBarStreamPosition.xaml
    /// </summary>
    [UIComponent("B54A1FAA-F507-4A17-9621-FC324AFF4CFE", role: UIComponentRole.Playback)]
    [UIComponentToolbar(1000, UIComponentToolbarAlignment.Stretch, false)]
    public partial class MoodBarStreamPosition : UIComponentBase
    {
        public const string CATEGORY = "BEE11F64-A91C-461C-9199-98854BF68708";

        public MoodBarStreamPosition()
        {
            this.InitializeComponent();
        }
    }
}
