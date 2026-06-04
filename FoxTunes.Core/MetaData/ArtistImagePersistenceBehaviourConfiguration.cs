using System.Collections.Generic;

namespace FoxTunes
{
    public static class ArtistImagePersistenceBehaviourConfiguration
    {
        public const string SECTION = "E7A1034C-B3E3-4983-8995-875074426CBB";

        public const string ENABLED = "928DF000-3E12-4FD5-BF80-1DCFDA917792";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.ArtistImagePersistenceBehaviourConfiguration_Enabled).WithValue(true)
            );
        }
    }
}
