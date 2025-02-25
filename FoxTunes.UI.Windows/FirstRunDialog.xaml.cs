using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for FirstRunDialog.xaml
    /// </summary>
    public partial class FirstRunDialog : UserControl
    {
        public FirstRunDialog()
        {
            this.InitializeComponent();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            var window = this.FindAncestor<Window>();
            window.Close();
        }
    }
}
