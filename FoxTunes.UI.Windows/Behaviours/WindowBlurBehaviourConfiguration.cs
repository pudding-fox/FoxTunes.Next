using System.Collections.Generic;

namespace FoxTunes
{
    public static class WindowBlurBehaviourConfiguration
    {
        public const string SECTION = WindowsUserInterfaceConfiguration.SECTION;

        public const string TRANSPARENCY_PROVIDER = WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new SelectionConfigurationElement(TRANSPARENCY_PROVIDER).WithOptions(GetProviders())
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetProviders()
        {
            yield return new SelectionConfigurationOption(WindowBlurBehaviour.ID, Strings.WindowBlurBehaviour_Name).Default();
        }
    }
}
