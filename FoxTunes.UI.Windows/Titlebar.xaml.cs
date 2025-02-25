using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#if NET40
using Microsoft.Windows.Shell;
#else
using System.Windows.Shell;
#endif

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Titlebar.xaml
    /// </summary>
    public partial class Titlebar : UserControl
    {
        public Titlebar()
        {
            this.InitializeComponent();
            this.Minimize.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
            this.MaximizeRestore.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
            this.Close.SetValue(WindowChrome.IsHitTestVisibleInChromeProperty, true);
        }
    }
}
