using System.Collections.Generic;

namespace FoxTunes
{
    public static class LRCLIBProviderConfiguration
    {
        public const string SECTION = LyricsBehaviourConfiguration.SECTION;

        public const string BASE_URL = "AAAA337F-9781-4446-A9E9-E1BE1F008DD9";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new TextConfigurationElement(BASE_URL, Strings.LRCLIBLyricsProviderConfiguration_BaseUrl, path: Strings.LRCLIB)
                    .WithValue(LRCLIBProvider.BASE_URL)
            );
        }
    }
}
