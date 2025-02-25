using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class UIComponentConfigurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UIComponentConfiguration config && targetType.IsAssignableFrom(typeof(string)))
            {
                return Serializer.SaveComponent(config);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string config && targetType.IsAssignableFrom(typeof(UIComponentConfiguration)))
            {
                if (string.IsNullOrEmpty(config))
                {
                    return new UIComponentConfiguration();
                }
                return Serializer.LoadComponent(config);
            }
            return value;
        }
    }
}
