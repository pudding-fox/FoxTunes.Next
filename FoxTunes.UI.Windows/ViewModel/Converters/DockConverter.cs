using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class DockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(typeof(Dock), global::System.Convert.ToString(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
