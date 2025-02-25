using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class ColumnWidthConverter : IValueConverter
    {
        public const double DEFAULT_WIDTH = 100;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(double).IsAssignableFrom(targetType))
            {
                if (value == null)
                {
                    return double.NaN;
                }
                else
                {
                    return value;
                }
            }
            if (typeof(bool?).IsAssignableFrom(targetType))
            {
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(double?).IsAssignableFrom(targetType))
            {
                if (object.Equals(value, true) || object.Equals(value, 0.0d) || object.Equals(value, double.NaN))
                {
                    return null;
                }
                if (object.Equals(value, false))
                {
                    return DEFAULT_WIDTH;
                }
                return value;
            }
            throw new NotImplementedException();
        }

        public static readonly IValueConverter Instance = new ColumnWidthConverter();
    }
}
