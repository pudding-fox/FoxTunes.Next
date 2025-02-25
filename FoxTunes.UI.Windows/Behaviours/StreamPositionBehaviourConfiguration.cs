using System.Collections.Generic;

namespace FoxTunes
{
    public static class StreamPositionBehaviourConfiguration
    {
        public const string SECTION = WindowsUserInterfaceConfiguration.SECTION;

        public const string SHOW_COUNTERS_ELEMENT = "MMMMAA77-6DCB-4DD2-9723-AD6201E31EB3";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                 .WithElement(
                    new BooleanConfigurationElement(SHOW_COUNTERS_ELEMENT, Strings.WindowsUserInterfaceConfiguration_Counters, path: Strings.General_Advanced).WithValue(false)
            );
        }
    }
}
