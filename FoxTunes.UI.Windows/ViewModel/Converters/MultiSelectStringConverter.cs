using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class MultiSelectStringConverter : IValueConverter
    {
        const char DELIMITER = '\t';

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                if (string.IsNullOrEmpty(text))
                {
                    return new List<string>();
                }
                return ToList(text);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable enumerable && typeof(string).IsAssignableFrom(targetType))
            {
                return ToString(enumerable);
            }
            return value;
        }

        protected virtual string ToString(IEnumerable enumerable)
        {
            var builder = new StringBuilder();
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (builder.Length > 0)
                {
                    builder.Append(DELIMITER);
                }
                builder.Append(enumerator.Current);
            }
            return builder.ToString();
        }

        protected virtual IList ToList(string text)
        {
            return text.Split(DELIMITER).ToList();
        }
    }
}
