using System.Collections.Generic;

namespace FoxTunes
{
    public static class MoodBarCacheConfiguration
    {
        public const string SECTION = MoodBarStreamPositionConfiguration.SECTION;

        public const string CACHE_ELEMENT = "BBBB6C24-6BA0-48B9-8455-1B1F717A383E";

        public const string CLEANUP_ELEMENT = "ZZZZDC1A-885A-48F2-9D8C-26C5B9394086";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MoodBarStreamPositionConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(CACHE_ELEMENT, Strings.MoodBarCacheConfiguration_Cache, path: Strings.General_Advanced).WithValue(true))
                .WithElement(new CommandConfigurationElement(CLEANUP_ELEMENT, Strings.MoodBarCacheConfiguration_Cleanup, path: Strings.General_Advanced)
                    .WithHandler(() => MoodBarCache.Cleanup())
                    .DependsOn(SECTION, CACHE_ELEMENT)
            );
        }
    }
}
