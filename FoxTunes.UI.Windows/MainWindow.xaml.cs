using FoxTunes.Interfaces;
using System.ComponentModel;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        public const string ID = "5B14F9E7-8759-45BA-834B-4F70E05CE22C";

        public const UserInterfaceWindowRole ROLE = UserInterfaceWindowRole.Main;

        public MainWindow()
        {
            if (!global::FoxTunes.Properties.Settings.Default.MainWindowBounds.IsEmpty())
            {
                if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.MainWindowBounds))
                {
                    this.Left = global::FoxTunes.Properties.Settings.Default.MainWindowBounds.Left;
                    this.Top = global::FoxTunes.Properties.Settings.Default.MainWindowBounds.Top;
                }
                this.Width = global::FoxTunes.Properties.Settings.Default.MainWindowBounds.Width;
                this.Height = global::FoxTunes.Properties.Settings.Default.MainWindowBounds.Height;
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

        public override UserInterfaceWindowRole Role
        {
            get
            {
                return ROLE;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            global::FoxTunes.Properties.Settings.Default.MainWindowBounds = this.RestoreBounds;
            global::FoxTunes.Properties.Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}
