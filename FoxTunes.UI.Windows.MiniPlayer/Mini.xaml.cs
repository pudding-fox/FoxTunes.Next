using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Mini.xaml
    /// </summary>
    public partial class Mini : UserControl
    {
        public Mini()
        {
            this.InitializeComponent();
        }

        protected virtual void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
