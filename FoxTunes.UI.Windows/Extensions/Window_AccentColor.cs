using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        public static readonly DependencyProperty AccentColorProperty = DependencyProperty.RegisterAttached(
            "AccentColor",
            typeof(Color),
            typeof(WindowExtensions),
            new FrameworkPropertyMetadata(default(Color), new PropertyChangedCallback(OnAccentColorPropertyChanged))
        );

        public static Color GetAccentColor(Window source)
        {
            return (Color)source.GetValue(AccentColorProperty);
        }

        public static void SetAccentColor(Window source, Color value)
        {
            source.SetValue(AccentColorProperty, value);
        }

        private static void OnAccentColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            var windowHelper = new WindowInteropHelper(window);
            WindowExtensions.EnableAcrylicBlur(windowHelper.Handle, GetAccentColor(window));
        }
    }
}
