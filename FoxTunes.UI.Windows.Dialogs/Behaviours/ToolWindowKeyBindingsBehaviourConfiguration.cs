using System.Collections.Generic;

namespace FoxTunes
{
    public static class ToolWindowKeyBindingsBehaviourConfiguration
    {
        public const string SECTION = InputManagerConfiguration.SECTION;

        public const string MANAGE_ELEMENT = "KKKKD258-38D5-4832-970F-DDB71F9EDBFD";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new TextConfigurationElement(MANAGE_ELEMENT, Strings.ToolWindowKeyBindingsBehaviourConfiguration_Manage)
                        .WithValue("Alt+D")
            );
        }
    }
}
