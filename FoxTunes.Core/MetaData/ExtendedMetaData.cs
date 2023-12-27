using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public static class ExtendedMetaData
    {
        public const string AmazonId = "AmazonId";
        public const string Comment = "Comment";
        public const string Copyright = "Copyright";
        public const string Grouping = "Grouping";
        public const string MusicIpId = "MusicIpId";

        public static IDictionary<string, string> Lookup = GetLookup();

        private static IDictionary<string, string> GetLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in typeof(ExtendedMetaData).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var name = field.GetValue(null) as string;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                lookup.Add(name, name);
                if (!string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    lookup.Add(field.Name, name);
                }
            }
            return lookup;
        }
    }
}
