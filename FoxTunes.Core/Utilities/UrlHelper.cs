using System;
using System.Collections.Generic;
using System.Text;

namespace FoxTunes
{
    public static class UrlHelper
    {
        public static string GetParameters(IDictionary<string, string> parameters)
        {
            var builder = new StringBuilder();
            foreach (var pair in parameters)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }
                if (builder.Length > 0)
                {
                    builder.Append("&");
                }
                builder.Append(pair.Key);
                builder.Append("=");
                builder.Append(EscapeDataString(pair.Value));
            }
            return builder.ToString();
        }

        public static string EscapeDataString(string value)
        {
            return Uri.EscapeDataString(value);
        }
    }
}
