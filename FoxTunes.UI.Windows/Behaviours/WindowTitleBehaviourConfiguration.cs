using System.Collections.Generic;

namespace FoxTunes
{
    public static class WindowTitleBehaviourConfiguration
    {
        public const string SECTION = WindowsUserInterfaceConfiguration.SECTION;

        public const string WINDOW_TITLE_SCRIPT_ELEMENT = "ZZZZ7FDC-6855-4C61-93BB-74A8057AED38";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new TextConfigurationElement(WINDOW_TITLE_SCRIPT_ELEMENT, "Window Title Script", path: "Advanced").WithValue(Resources.NowPlaying).WithFlags(ConfigurationElementFlags.Script)
            );
        }
    }
}
