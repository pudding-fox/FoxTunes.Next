using System.Collections.Generic;

namespace FoxTunes
{
    public static class AIPlaylistBehaviourConfiguration
    {
        public const string SECTION = "E599001B-8965-4BC9-96FA-9F76341F6166";

        public const string MODEL_LOCATION = "AAAACCF4-9CA6-45DE-A3B1-AE80CD52817A";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AIPlaylistBehaviourConfiguration_Section)
                .WithElement(new TextConfigurationElement(MODEL_LOCATION, Strings.AIPlaylistBehaviourConfiguration_ModelLocation)
            );
        }
    }
}
