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
            if (value is string text && typeof(IList).IsAssignableFrom(targetType))
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
            if (value is IList list && typeof(string).IsAssignableFrom(targetType))
            {
                if (list.Count == 0)
                {
                    return string.Empty;
                }
                return ToString(list);
            }
            return value;
        }

        protected virtual string ToString(IList list)
        {
            var builder = new StringBuilder();
            foreach (var element in list)
            {
                if (builder.Length > 0)
                {
                    builder.Append(DELIMITER);
                }
                builder.Append(element);
            }
            return builder.ToString();
        }

        protected virtual IList ToList(string text)
        {
            return text.Split(DELIMITER).ToList();
        }
    }
}
