using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public class DateHelper
    {
        public static string toLocaleDateString(string value)
        {
            var date = DateTimeHelper.FromString(value);
            if (date == default(DateTime))
            {
                return null;
            }
            return date.ToShortDateString() + " " + date.ToShortTimeString();
        }
    }

    public class NumberHelper
    {
        public static float parseFloat(string value)
        {
            var parsed = default(float);
            if (float.TryParse(value, out parsed))
            {
                return parsed;
            }
            return 0.0f;
        }

        public static string toFixed(float value, int digits)
        {
            return value.ToString(string.Format("N{0}", digits));
        }
    }

    public static class StringsHelper
    {
        public static readonly Dictionary<string, object> Strings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        static StringsHelper()
        {
            try
            {
                var properties = typeof(Strings).GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var property in properties)
                {
                    if (property.PropertyType != typeof(string))
                    {
                        //Not a resource string.
                        continue;
                    }
                    var key = property.Name.ToLower();
                    if (Strings.ContainsKey(key))
                    {
                        //Ambiguous name.
                        continue;
                    }
                    var value = property.GetValue(null, null);
                    Strings.Add(key, value);
                }
            }
            catch
            {
                //Don't throw during initialization, nothing can be done.
            }
        }
    }
}
