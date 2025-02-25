using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class MetaDataNameConverter : IValueConverter
    {
        public static readonly Regex Pattern = new Regex(@"\B([A-Z])", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                var localized = Strings.ResourceManager.GetString(string.Format("MetaDataName.{0}", text));
                if (!string.IsNullOrEmpty(localized))
                {
                    return localized;
                }
                //If not localized then format sentence case.
                return Pattern.Replace(text, " $1");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
