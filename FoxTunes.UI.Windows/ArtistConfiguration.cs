using System.Collections.Generic;

namespace FoxTunes
{
    public static class ArtistConfiguration
    {
        public static string SECTION = "620D62A4-EAF9-4D75-BC62-CDEEF4457846";

        public static string BLUR = "AAAAD918-24CD-4D50-B8C9-26EF0E1F6D98";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.StaticImageConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(BLUR, Strings.ArtworkConfiguration_Blur));
        }
    }
}
