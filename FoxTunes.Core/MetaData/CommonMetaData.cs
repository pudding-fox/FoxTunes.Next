﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public static class CommonMetaData
    {
        public const string Album = "Album";
        public const string Artist = "Artist";
        public const string BeatsPerMinute = "BeatsPerMinute";
        public const string Composer = "Composer";
        public const string Conductor = "Conductor";
        public const string Disc = "Disc";
        public const string DiscCount = "DiscCount";
        public const string Genre = "Genre";
        public const string InitialKey = "InitialKey";
        public const string IsCompilation = "IsCompilation";
        public const string Lyrics = "Lyrics";
        public const string Performer = "Performer";
        public const string ReplayGainAlbumGain = "ReplayGainAlbumGain";
        public const string ReplayGainAlbumPeak = "ReplayGainAlbumPeak";
        public const string ReplayGainTrackGain = "ReplayGainTrackGain";
        public const string ReplayGainTrackPeak = "ReplayGainTrackPeak";
        public const string Title = "Title";
        public const string Track = "Track";
        public const string TrackCount = "TrackCount";
        public const string Year = "Year";

        public static IDictionary<string, string> Lookup = GetLookup();

        private static IDictionary<string, string> GetLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in typeof(CommonMetaData).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var name = field.GetValue(null) as string;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                lookup.Add(name, name);
            }
            return lookup;
        }
    }
}
