using System.Collections.Generic;

namespace FoxTunes
{
    public static class DefaultKeyBindingsBehaviourConfiguration
    {
        public const string SECTION = InputManagerConfiguration.SECTION;

        public const string PLAY_ELEMENT = "AAAA39A4-A260-4AA8-8E3F-E0ECD5C6727C";

        public const string PREVIOUS_ELEMENT = "BBBBC8F4-5F5B-4260-B7A5-F9829F4C8DF1";

        public const string NEXT_ELEMENT = "CCCCAF62-A5C5-4C6B-804F-47E086963B87";

        public const string STOP_ELEMENT = "DDDD3853-09F5-4DF7-9CDD-918B7B2A5F22";

        public const string SETTINGS_ELEMENT = "EEEE5C74-24D3-4C51-B50A-74EC4921FDC7";

        public const string SEARCH_ELEMENT = "GGGG6849-7DA9-4BA1-9A5A-548E3D9A1E25";

        public const string EQUALIZER_ELEMENT = "HHHHFEB5-DD2D-4332-9C2A-F5EF41F71B40";

        public const string FULL_SCREEN_ELEMENT = "IIII9FDD-6588-4BC5-AC6F-735070E770F6";

        public const string PLAYLIST_MANAGER = "JJJJAC36-BE1F-4B67-90C1-1DDFCCB64C8F";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.KeyBindingsBehaviourConfiguration_Section)
                .WithElement(
                    new TextConfigurationElement(PLAY_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Play)
                        .WithValue("Alt+Space"))
                .WithElement(
                    new TextConfigurationElement(PREVIOUS_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Previous)
                        .WithValue("Alt+Left"))
                 .WithElement(
                    new TextConfigurationElement(NEXT_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Next)
                        .WithValue("Alt+Right"))
                .WithElement(
                    new TextConfigurationElement(STOP_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Stop)
                        .WithValue("Alt+Back"))
                .WithElement(
                    new TextConfigurationElement(SETTINGS_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Settings)
                        .WithValue("Alt+S"))
                .WithElement(
                    new TextConfigurationElement(SEARCH_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Search)
                        .WithValue("Alt+F"))
                .WithElement(
                    new TextConfigurationElement(EQUALIZER_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_Equalizer)
                        .WithValue("Alt+E"))
                .WithElement(
                    new TextConfigurationElement(FULL_SCREEN_ELEMENT, Strings.KeyBindingsBehaviourConfiguration_FullScreen)
                        .WithValue("F11"))
                .WithElement(
                    new TextConfigurationElement(PLAYLIST_MANAGER, Strings.KeyBindingsBehaviourConfiguration_PlaylistManager)
                        .WithValue("Alt+P")
            );
        }
    }
}
