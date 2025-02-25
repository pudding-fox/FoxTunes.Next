using System;
using System.Globalization;

namespace FoxTunes
{
    public static class DateTimeHelper
    {
        public static readonly string NEVER = ToString(new DateTime(1990, 01, 01));

        public static string ToString(DateTime value)
        {
            return value.ToString(Constants.DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        public static string ToShortString(DateTime value)
        {
            return value.ToString(Constants.SHORT_DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        public static DateTime FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default(DateTime);
            }
            if (string.Equals(value, NEVER, StringComparison.OrdinalIgnoreCase))
            {
                return default(DateTime);
            }
            var date = default(DateTime);
            if (DateTime.TryParseExact(value, Constants.DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
            {
                return date;
            }
            return default(DateTime);
        }
    }
}
