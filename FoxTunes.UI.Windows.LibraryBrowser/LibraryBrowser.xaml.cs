using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for LibraryBrowser.xaml
    /// </summary>
    [UIComponent(ID, role: UIComponentRole.Library)]
    public partial class LibraryBrowser : LibraryBrowserBase
    {
        public const string ID = "FB75ECEC-A89A-4DAD-BA8D-9DB43F3DE5E3";

        public LibraryBrowser()
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
