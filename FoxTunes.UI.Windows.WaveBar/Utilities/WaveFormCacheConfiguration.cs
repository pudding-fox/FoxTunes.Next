using System.Collections.Generic;

namespace FoxTunes
{
    public static class WaveFormCacheConfiguration
    {
        public const string SECTION = WaveFormStreamPositionConfiguration.SECTION;

        public const string CACHE_ELEMENT = "BBBBAD4B-8BB4-47C9-9D18-2121C48115CE";

        public const string CLEANUP_ELEMENT = "ZZZZ8740-576A-4456-A2CC-0B1E7DEF6913";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.WaveFormStreamPositionConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(CACHE_ELEMENT, Strings.WaveFormCacheConfiguration_Cache, path: Strings.General_Advanced).WithValue(true))
                .WithElement(new CommandConfigurationElement(CLEANUP_ELEMENT, Strings.WaveFormCacheConfiguration_Cleanup, path: Strings.General_Advanced)
                    .WithHandler(() =>
                    {
                        WaveFormCache.Cleanup();
                        BandedWaveFormCache.Cleanup();
                    })
                    .DependsOn(SECTION, CACHE_ELEMENT)
            );
        }
    }
}
