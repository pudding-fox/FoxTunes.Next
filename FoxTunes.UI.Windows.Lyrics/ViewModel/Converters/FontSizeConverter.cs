using FoxTunes.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class FontSizeConverter : IValueConverter
    {
        public static readonly IConfigurationBase Configuration = ComponentRegistry.Instance.GetComponent<IConfigurationBase>();

        public static readonly DoubleConfigurationElement FontSize = Configuration.GetElement<DoubleConfigurationElement>(
            WindowsUserInterfaceConfiguration.SECTION,
            WindowsUserInterfaceConfiguration.FONT_SIZE
        );

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FontSize.Value + global::System.Convert.ToInt32(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
