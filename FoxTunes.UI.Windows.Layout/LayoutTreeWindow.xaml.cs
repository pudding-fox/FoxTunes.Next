using System.ComponentModel;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for LayoutTreeWindow.xaml
    /// </summary>
    public partial class LayoutTreeWindow : WindowBase
    {
        public const string ID = "345100C4-3E83-417C-963C-8E6F88902285";

        public LayoutTreeWindow()
        {
            if (!global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds.IsEmpty())
            {
                if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds))
                {
                    this.Left = global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds.Left;
                    this.Top = global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds.Top;
                }
                this.Width = global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds.Width;
                this.Height = global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds.Height;
            }
            else
            {
                this.Width = 300;
                this.Height = 600;
            }
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            this.InitializeComponent();
        }

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            global::FoxTunes.Properties.Settings.Default.LayoutTreeWindowBounds = this.RestoreBounds;
            global::FoxTunes.Properties.Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}
