using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static class Presets
    {
        public static readonly IConfiguration Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();

        public static readonly BooleanConfigurationElement Low = Configuration.GetElement<BooleanConfigurationElement>(
            ProjectMBehaviourConfiguration.SECTION,
            ProjectMBehaviourConfiguration.COMPLEXITY_LOW
        );

        public static readonly BooleanConfigurationElement Medium = Configuration.GetElement<BooleanConfigurationElement>(
            ProjectMBehaviourConfiguration.SECTION,
            ProjectMBehaviourConfiguration.COMPLEXITY_MEDIUM
        );

        public static readonly BooleanConfigurationElement High = Configuration.GetElement<BooleanConfigurationElement>(
            ProjectMBehaviourConfiguration.SECTION,
            ProjectMBehaviourConfiguration.COMPLEXITY_HIGH
        );

        public static readonly BooleanConfigurationElement Extreme = Configuration.GetElement<BooleanConfigurationElement>(
            ProjectMBehaviourConfiguration.SECTION,
            ProjectMBehaviourConfiguration.COMPLEXITY_EXTREME
        );

        static Presets()
        {
            FileNames = FileSystemHelper.EnumerateFiles(
                Path.Combine(Location, "Presets"),
                "*.milk",
                FileSystemHelper.SearchOption.Recursive
            ).Select(file => new PresetInfo(file)).ToList();
            FileNames.Shuffle();
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(Presets).Assembly.Location);
            }
        }

        public static int Index { get; private set; }

        public static string Search { get; set; }

        public static IList<PresetInfo> FileNames { get; private set; }

        public static ConcurrentDictionary<string, string> Complexity { get; private set; }

        private static bool MatchesSearch(PresetInfo preset)
        {
            return
                string.IsNullOrEmpty(Search) ||
                preset.FileName.Contains(Search, true);
        }

        private static bool MatchesComplexity(PresetInfo preset)
        {
            return
                Low.Value && string.Equals(preset.Complexity, "Low", StringComparison.OrdinalIgnoreCase) ||
                Medium.Value && string.Equals(preset.Complexity, "Medium", StringComparison.OrdinalIgnoreCase) ||
                High.Value && string.Equals(preset.Complexity, "High", StringComparison.OrdinalIgnoreCase) ||
                Extreme.Value && string.Equals(preset.Complexity, "Extreme", StringComparison.OrdinalIgnoreCase);
        }

        public static string Next()
        {
            for (var a = 0; a < FileNames.Count; a++)
            {
                Index = (Index + 1) % FileNames.Count;
                var preset = FileNames[Index];
                if (MatchesSearch(preset) && MatchesComplexity(preset))
                {
                    return preset.FileName;
                }
            }
            return string.Empty;
        }

        public static string Previous()
        {
            for (var a = 0; a < FileNames.Count; a++)
            {
                Index = (Index - 1 + FileNames.Count) % FileNames.Count;
                var preset = FileNames[Index];
                if (MatchesSearch(preset) && MatchesComplexity(preset))
                {
                    return preset.FileName;
                }
            }
            return string.Empty;
        }

        public class PresetInfo
        {
            public PresetInfo(string fileName)
            {
                this.FileName = fileName;
            }

            public string FileName { get; }

            private string _Complexity { get; set; }

            public string Complexity
            {
                get
                {
                    if (string.IsNullOrEmpty(this._Complexity))
                    {
                        this._Complexity = PresetComplexity.Analyze(this.FileName);
                    }
                    return this._Complexity;
                }
            }
        }
    }
}