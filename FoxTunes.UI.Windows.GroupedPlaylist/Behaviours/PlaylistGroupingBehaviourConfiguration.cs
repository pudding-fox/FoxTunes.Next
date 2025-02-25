using System.Collections.Generic;

namespace FoxTunes
{
    public static class PlaylistGroupingBehaviourConfiguration
    {
        public const string GROUP_SCRIPT_ELEMENT = "CE8245BF-9BE2-4853-9E2F-40389293EFED";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(PlaylistBehaviourConfiguration.SECTION)
                .WithElement(
                    new TextConfigurationElement(GROUP_SCRIPT_ELEMENT, Strings.PlaylistGroupingBehaviourConfiguration_Script, path: Strings.General_Advanced)
                        .WithValue(Resources.Grouping)
                        .WithFlags(ConfigurationElementFlags.MultiLine)
            );
        }
    }
}