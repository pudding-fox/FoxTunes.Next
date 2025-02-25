using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var boolean = default(bool);
                if (bool.TryParse(text, out boolean))
                {
                    return boolean;
                }
            }
            //TODO: Indeterminate?
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                //boolean.ToString()?
                return boolean ? bool.TrueString : bool.FalseString;
            }
            return value;
        }
    }
}
