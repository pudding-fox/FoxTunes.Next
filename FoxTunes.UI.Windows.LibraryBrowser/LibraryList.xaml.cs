using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for LibraryBrowser.xaml
    /// </summary>
    [UIComponent(ID, role: UIComponentRole.Library)]
    public partial class LibraryList : LibraryBrowserBase
    {
        public const string ID = "435999ED-83D7-45AF-9CBF-A99DBE842EB3";

        public LibraryList()
        {
            this.InitializeComponent();
        }

        protected override void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
#if NET40

#else
            VirtualizingPanel.SetScrollUnit(sender as DependencyObject, ScrollUnit.Pixel);
#endif
            base.OnListBoxLoaded(sender, e);
        }

        protected override ItemsControl GetItemsControl()
        {
            return this.ItemsControl;
        }

        protected override MouseCursorAdorner GetMouseCursorAdorner()
        {
            return this.MouseCursorAdorner;
        }
    }
}
