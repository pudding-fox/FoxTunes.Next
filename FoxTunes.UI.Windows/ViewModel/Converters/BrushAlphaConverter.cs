using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FoxTunes.ViewModel
{
    internal class BrushAlphaConverter : ViewModelBase, IValueConverter
    {
        public static readonly ThemeLoader ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ThemeLoader.Theme != null && ThemeLoader.Theme.Opacity < 1.0f)
            {
                if (value is SolidColorBrush brush)
                {
                    var color = brush.Color;
                    var opacity = (byte)((ThemeLoader.Theme.Opacity * 3) * byte.MaxValue);
                    return new SolidColorBrush(Color.FromArgb(opacity, color.R, color.G, color.B));
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BrushAlphaConverter();
        }
    }
}
