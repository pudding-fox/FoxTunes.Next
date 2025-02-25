using FoxDb;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class WrapperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = FastActivator.Instance.Activate(targetType);
            if (value is IEnumerable sequence && result is IList list)
            {
                foreach (var element in sequence.OfType<Wrapper>())
                {
                    var index = list.Add(element.Value);
                    //TODO: This is awful and probably leaks.
                    element.ValueChanged += (sender, e) =>
                    {
                        list[index] = element.Value;
                    };
                }
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
