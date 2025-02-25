using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class GridLengthConverter : System.Windows.GridLengthConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return this.ConvertFrom(null, culture, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength)
            {
                var length = (GridLength)value;
                if (length.IsStar)
                {
                    value = new GridLength(length.Value, GridUnitType.Pixel);
                }
            }
            return this.ConvertTo(null, culture, value, targetType);
        }
    }
}
