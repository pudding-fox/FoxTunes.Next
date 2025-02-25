using System.Collections.Generic;

namespace FoxTunes
{
    public static class LRCLIBProviderConfiguration
    {
        public const string SECTION = LyricsBehaviourConfiguration.SECTION;

        public const string AUTO_LOOKUP = LyricsBehaviourConfiguration.AUTO_LOOKUP;

        public const string AUTO_LOOKUP_PROVIDER = LyricsBehaviourConfiguration.AUTO_LOOKUP_PROVIDER;

        public const string BASE_URL = "AAAA337F-9781-4446-A9E9-E1BE1F008DD9";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new SelectionConfigurationElement(AUTO_LOOKUP_PROVIDER)
                    .WithOptions(new[] { new SelectionConfigurationOption(LRCLIBProvider.ID, Strings.LRCLIB) })
                    .DependsOn(SECTION, AUTO_LOOKUP))
                .WithElement(new TextConfigurationElement(BASE_URL, Strings.LRCLIBLyricsProviderConfiguration_BaseUrl, path: Strings.LRCLIB)
                    .WithValue(LRCLIBProvider.BASE_URL)
            );
        }
    }
}
