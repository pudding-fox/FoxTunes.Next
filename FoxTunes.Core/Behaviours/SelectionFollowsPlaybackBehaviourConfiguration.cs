using System.Collections.Generic;

namespace FoxTunes
{
    public static class SelectionFollowsPlaybackBehaviourConfiguration
    {
        public const string SECTION = PlaylistBehaviourConfiguration.SECTION;

        public const string SELECTION_FOLLOWS_PLAYBACK = "HHHH5FED-C125-463A-8FC9-909D6DCA0756";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new BooleanConfigurationElement(SELECTION_FOLLOWS_PLAYBACK, Strings.SelectionFollowsPlaybackBehaviourConfiguration_SelectionFollowsPlayback).WithValue(false)
            );
        }
    }
}
