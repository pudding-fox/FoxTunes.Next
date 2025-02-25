using FoxDb;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class IntegerToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var value1 = Converter.ChangeType<int>(value);
            var value2 = 0;
            if (parameter != null)
            {
                value2 = Converter.ChangeType<int>(parameter);
            }
            return value1 >= value2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
