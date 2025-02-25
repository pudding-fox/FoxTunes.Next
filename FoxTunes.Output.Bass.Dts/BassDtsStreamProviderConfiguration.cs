using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassDtsStreamProviderConfiguration
    {
        public const string SECTION = "91103CC0-6E88-4EE5-88F6-6F96F069BFC0";

        public const string PROBE_WAV_FILES = "AAAA1565-7F80-4736-BE11-FF1EFA226059";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassDtsStreamProviderConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(PROBE_WAV_FILES, Strings.BassDtsStreamProviderConfiguration_ProbeWaveFiles));
        }
    }
}
