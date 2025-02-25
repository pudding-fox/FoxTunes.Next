using System.Collections.Generic;

namespace FoxTunes
{
    public static class NotifyIconConfiguration
    {
        public const string SECTION = "F9B3FAE5-87BD-486F-9C23-B8B11A8FDAA9";

        public const string ENABLED_ELEMENT = "AAAA1AC8-7D75-43C9-9E99-FF69EC5D8040";

        public const string MINIMIZE_TO_TRAY_ELEMENT = "BBBB2156-2E0E-4EA3-AFAA-6775CF3421F3";

        public const string CLOSE_TO_TRAY_ELEMENT = "CCCC012A-C958-4964-9574-D57196F36D21";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.NotifyIconConfiguration_Section)
                .WithElement(
                    new BooleanConfigurationElement(ENABLED_ELEMENT, Strings.NotifyIconConfiguration_Enabled).WithValue(false))
                .WithElement(
                    new BooleanConfigurationElement(MINIMIZE_TO_TRAY_ELEMENT, Strings.NotifyIconConfiguration_Minimize).WithValue(true).DependsOn(SECTION, ENABLED_ELEMENT))
                .WithElement(
                    new BooleanConfigurationElement(CLOSE_TO_TRAY_ELEMENT, Strings.NotifyIconConfiguration_Close).WithValue(false).DependsOn(SECTION, ENABLED_ELEMENT)
            );
        }
    }
}
