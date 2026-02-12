using FoxTunes.Interfaces;
using System.ComponentModel;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for MiniWindow.xaml
    /// </summary>
    public partial class MiniWindow : WindowBase
    {
        public const string ID = "95FA900C-2B6C-4571-B119-D24834E2FC22";

        public const UserInterfaceWindowRole ROLE = UserInterfaceWindowRole.Main;

        public MiniWindow()
        {
            if (!global::FoxTunes.Properties.Settings.Default.MiniWindowBounds.IsEmpty())
            {
                if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.MiniWindowBounds))
                {
                    this.Left = global::FoxTunes.Properties.Settings.Default.MiniWindowBounds.Left;
                    this.Top = global::FoxTunes.Properties.Settings.Default.MiniWindowBounds.Top;
                }
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

        protected override bool ApplyTemplate
        {
            get
            {
                return false;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            base.OnMouseDown(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            global::FoxTunes.Properties.Settings.Default.MiniWindowBounds = this.RestoreBounds;
            global::FoxTunes.Properties.Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}
