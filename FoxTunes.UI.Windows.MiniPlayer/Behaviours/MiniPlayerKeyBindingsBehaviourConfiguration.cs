using System.Collections.Generic;

namespace FoxTunes
{
    public static class MiniPlayerKeyBindingsBehaviourConfiguration
    {
        public const string SECTION = DefaultKeyBindingsBehaviourConfiguration.SECTION;

        public const string TOGGLE = "FFFFDF70-E0DB-4154-9567-01AE394BA476";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new TextConfigurationElement(TOGGLE, Strings.MiniPlayerKeyBindingsBehaviourConfiguration_Toggle).WithValue("Alt+M")
            );
        }
    }
}
