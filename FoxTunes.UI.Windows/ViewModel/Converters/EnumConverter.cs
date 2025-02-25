using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var type = value.GetType();
                if (type.IsEnum)
                {
                    var name = Enum.GetName(type, value);
                    var localized = Strings.ResourceManager.GetString(string.Format("{0}.{1}", type.Name, name));
                    if (!string.IsNullOrEmpty(localized))
                    {
                        return localized;
                    }
                    return name;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
