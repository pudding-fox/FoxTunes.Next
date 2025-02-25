using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class NegatingBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter BooleanToVisibilityConverter = new BooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                value = !(bool)value;
            }
            return BooleanToVisibilityConverter.Convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
