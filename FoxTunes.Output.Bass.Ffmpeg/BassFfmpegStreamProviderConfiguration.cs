using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassDtsStreamProviderConfiguration
    {
        public const string SECTION = "05F831B9-B363-48CA-98F4-9D01C7750BD3";

        public const string EXTENSIONS = "AAAAD14-89E4-47C0-8ED0-6D9928AF3DC1";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassFfmpegStreamProviderConfiguration_Section)
                .WithElement(new TextConfigurationElement(EXTENSIONS, Strings.BassFfmpegStreamProviderConfiguration_Extensions));
        }
    }
}
