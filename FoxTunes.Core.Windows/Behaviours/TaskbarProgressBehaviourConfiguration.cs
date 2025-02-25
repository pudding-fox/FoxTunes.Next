using System.Collections.Generic;

namespace FoxTunes
{
    public static class TaskbarProgressBehaviourConfiguration
    {
        public const string SECTION = WindowsConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "BBBB676E-ACEA-4D5F-8B1D-B02758CAE959";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
                yield return new ConfigurationSection(SECTION, Strings.WindowsConfiguration_Section)
                    .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, Strings.TaskbarProgressBehaviourConfiguration_Enabled).WithValue(Publication.ReleaseType == ReleaseType.Default)
                );
        }
    }
}
