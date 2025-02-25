using System.Collections.Generic;

namespace FoxTunes
{
    public static class NowPlayingConfiguration
    {
        public const string SECTION = "A9F63A1C-16F8-4F68-8E49-3C4C62172FFA";

        public const string NOW_PLAYING_SCRIPT_ELEMENT = "BBBB78F3-B32F-4A8C-B566-9A8B39A896C7";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.NowPlaying_Name)
                .WithElement(
                    new TextConfigurationElement(NOW_PLAYING_SCRIPT_ELEMENT, Strings.MiniPlayerBehaviourConfiguration_NowPlayingScript, path: Strings.MiniPlayerBehaviourConfiguration_Advanced)
                    .WithValue(Resources.NowPlaying)
                    .WithFlags(ConfigurationElementFlags.MultiLine | ConfigurationElementFlags.Script)
            );
        }
    }
}
