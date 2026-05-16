using System.Collections.Generic;

namespace FoxTunes
{
    public static class InputManagerConfiguration
    {
        public const string SECTION = "4B5B0E73-8000-484E-8F68-77E11FC8AD45";

        public const string ENABLED = "AAAA41A0-976D-4573-8758-984AACBD235B";

        public const string PLAY = "BBBBD9B9-1D10-4EEB-81BA-7D54C7A86198";

        public const string PREVIOUS = "CCCC497E-32FA-4E0F-B73F-8BF4705782B0";

        public const string NEXT = "DDDD25C3-6A2C-4D2C-985B-1D7210304EFB";

        public const string STOP = "EEEE73AA-0388-4988-8068-59B768F2A02E";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.InputManagerConfiguration_Section)
                .WithElement(
                    new BooleanConfigurationElement(ENABLED, Strings.InputManagerConfiguration_Enabled, path: Strings.InputManagerConfiguration_Path_GlobalHotkeys).WithValue(false))
                .WithElement(
                    new TextConfigurationElement(PLAY, Strings.InputManagerConfiguration_Play, path: Strings.InputManagerConfiguration_Path_GlobalHotkeys)
                        .WithValue("MediaPlayPause").DependsOn(SECTION, ENABLED))
                .WithElement(
                    new TextConfigurationElement(PREVIOUS, Strings.InputManagerConfiguration_Previous, path: Strings.InputManagerConfiguration_Path_GlobalHotkeys)
                        .WithValue("MediaPreviousTrack").DependsOn(SECTION, ENABLED))
                 .WithElement(
                    new TextConfigurationElement(NEXT, Strings.InputManagerConfiguration_Next, path: Strings.InputManagerConfiguration_Path_GlobalHotkeys)
                        .WithValue("MediaNextTrack").DependsOn(SECTION, ENABLED))
                .WithElement(
                    new TextConfigurationElement(STOP, Strings.InputManagerConfiguration_Stop, path: Strings.InputManagerConfiguration_Path_GlobalHotkeys)
                        .WithValue("MediaStop").DependsOn(SECTION, ENABLED)
            );
        }
    }
}
