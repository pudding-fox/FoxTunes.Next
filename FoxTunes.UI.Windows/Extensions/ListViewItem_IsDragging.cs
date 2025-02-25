using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public partial class ListViewItemExtensions
    {
        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.RegisterAttached(
            "IsDragging",
            typeof(bool),
            typeof(ListViewItemExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsDraggingPropertyChanged))
        );

        public static bool GetIsDragging(ListViewItem source)
        {
            return (bool)source.GetValue(IsDraggingProperty);
        }

        public static void SetIsDragging(ListViewItem source, bool value)
        {
            source.SetValue(IsDraggingProperty, value);
        }

        private static void OnIsDraggingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            //Nothing to do.
        }
    }
}
