using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class WindowStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is global::System.Windows.WindowState windowState)
            {
                switch (windowState)
                {
                    case global::System.Windows.WindowState.Maximized:
                        return "\u0032";
                    default:
                        return "\u0031";
                }
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
