using System.Collections.Generic;

namespace FoxTunes
{
    public static class WindowAcrylicBlurBehaviourConfiguration
    {
        public const string SECTION = WindowsUserInterfaceConfiguration.SECTION;

        public const string TRANSPARENCY_PROVIDER = WindowsUserInterfaceConfiguration.TRANSPARENCY_PROVIDER;

        public const string ACCENT_COLOR = "OOOO5DBB-8ACE-4FFC-B975-9131D4D82947";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new SelectionConfigurationElement(TRANSPARENCY_PROVIDER)
                        .WithOptions(GetProviders()))
                .WithElement(
                    new TextConfigurationElement(ACCENT_COLOR, Strings.WindowsUserInterfaceConfiguration_AccentColor)
                        .DependsOn(SECTION, TRANSPARENCY_PROVIDER, WindowAcrylicBlurBehaviour.ID)
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetProviders()
        {
            yield return new SelectionConfigurationOption(WindowAcrylicBlurBehaviour.ID, Strings.WindowAcrylicBlurBehaviour_Name);
        }
    }
}
