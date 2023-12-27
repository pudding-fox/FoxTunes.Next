using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassMemoryBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "OOOOBED1-7965-47A3-9798-E46124386A8D";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, Strings.BassMemoryStreamComponentBehaviourConfiguration_Enabled, path: Strings.General_Advanced).WithValue(false)
            );
        }
    }
}
