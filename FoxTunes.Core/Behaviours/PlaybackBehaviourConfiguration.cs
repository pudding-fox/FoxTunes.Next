using System.Collections.Generic;

namespace FoxTunes
{
    public static class PlaybackBehaviourConfiguration
    {
        public const string SECTION = "3BA15137-8846-4482-883F-53B20023F26B";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.PlaybackBehaviourConfiguration_Section);
        }
    }
}
