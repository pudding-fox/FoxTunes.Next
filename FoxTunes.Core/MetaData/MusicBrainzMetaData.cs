using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public static class MusicBrainzMetaData
    {
        public const string MusicBrainzArtistId = "MusicBrainzArtistId";
        public const string MusicBrainzDiscId = "MusicBrainzDiscId";
        public const string MusicBrainzReleaseArtistId = "MusicBrainzReleaseArtistId";
        public const string MusicBrainzReleaseCountry = "MusicBrainzReleaseCountry";
        public const string MusicBrainzReleaseId = "MusicBrainzReleaseId";
        public const string MusicBrainzReleaseStatus = "MusicBrainzReleaseStatus";
        public const string MusicBrainzReleaseType = "MusicBrainzReleaseType";
        public const string MusicBrainzTrackId = "MusicBrainzTrackId";

        public static IDictionary<string, string> Lookup = GetLookup();

        private static IDictionary<string, string> GetLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in typeof(MusicBrainzMetaData).GetFields(BindingFlags.Public | BindingFlags.Static))
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
