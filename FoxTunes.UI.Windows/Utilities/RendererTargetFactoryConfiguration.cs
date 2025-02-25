using System.Collections.Generic;

namespace FoxTunes
{
    public static class RendererTargetFactoryConfiguration
    {
        public const string SECTION = "513404FF-7FEB-46B5-9404-6D90090B3543";

        public const string BACKEND = "AAAAF238-1361-44E9-8D21-608A1CB12362";

        public const string PRIORITY = "BBBBA664-CEBD-48EB-A507-E455CCA3E336";

        public const string PRIORITY_LOW = "AAAAEED1-4D92-40A8-B102-04D57665D27D";

        public const string PRIORITY_NORMAL = "BBBBEED1-4D92-40A8-B102-04D57665D27D";

        public const string PRIORITY_HIGH = "CCCC01A3-F90F-4513-9762-23EBCD8CCC19";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.RendererTargetFactoryConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(BACKEND, Strings.RendererTargetFactoryConfiguration_Backend))
                .WithElement(new SelectionConfigurationElement(PRIORITY, Strings.RendererTargetFactoryConfiguration_Priority).WithOptions(GetPriorities()));
        }

        private static IEnumerable<SelectionConfigurationOption> GetPriorities()
        {
            yield return new SelectionConfigurationOption(PRIORITY_LOW, Strings.RendererTargetFactoryConfiguration_Priority_Low);
            yield return new SelectionConfigurationOption(PRIORITY_NORMAL, Strings.RendererTargetFactoryConfiguration_Priority_Normal).Default();
            yield return new SelectionConfigurationOption(PRIORITY_HIGH, Strings.RendererTargetFactoryConfiguration_Priority_High);
        }
    }
}
