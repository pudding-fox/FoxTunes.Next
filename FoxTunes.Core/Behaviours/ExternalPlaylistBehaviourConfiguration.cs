using System.Collections.Generic;

namespace FoxTunes
{
    public static class ExternalPlaylistBehaviourConfiguration
    {
        public const string SECTION = PlaylistBehaviourConfiguration.SECTION;

        public const string ENABLED = "HHHH6F42-EC43-405A-9542-7DE5EAC0A718";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.ExternalPlaylistBehaviourConfiguration_Enabled).WithValue(false));
        }
    }
}
