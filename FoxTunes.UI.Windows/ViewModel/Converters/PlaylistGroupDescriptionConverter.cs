using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class PlaylistGroupDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var groupName = global::System.Convert.ToString(value);
            var parts = groupName.Split('\t');
            return parts.LastOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
