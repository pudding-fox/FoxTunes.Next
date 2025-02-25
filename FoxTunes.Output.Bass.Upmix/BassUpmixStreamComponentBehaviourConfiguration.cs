using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassUpmixStreamComponentBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED = "000C17DA-C79F-405C-AA17-AF7BC5943F2F";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.BassUpmixStreamComponentBehaviourConfiguration_Enabled, path: Strings.General_Advanced).WithValue(false)
            );
        }
    }
}
