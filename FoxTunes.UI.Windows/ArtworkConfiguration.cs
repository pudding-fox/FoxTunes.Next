using System.Collections.Generic;

namespace FoxTunes
{
    public static class ArtworkConfiguration
    {
        public static string SECTION = "2561D272-6C3F-4605-814D-D02EF28E2B34";

        public static string BLUR = "AAAACE6A-AB50-4B4E-89C3-184C94FB0C08";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.StaticImageConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(BLUR, Strings.ArtworkConfiguration_Blur));
        }
    }
}
