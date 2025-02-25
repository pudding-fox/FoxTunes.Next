using System.Collections.Generic;

namespace FoxTunes
{
    public static class SelectionPropertiesConfiguration
    {
        public const string SECTION = "B059D3DB-075B-4CDA-84B4-BE8414EA1D28";

        public const string SHOW_TAGS = "AAAA125A-AC00-43A2-8F25-510FC9E2F423";

        public const string SHOW_PROPERTIES = "BBBB6B1C-5587-4260-966F-DBFC2BAB474D";

        public const string SHOW_REPLAYGAIN = "BBCC9137-2B5C-4D2E-9976-A268C1AEB146";

        public const string SHOW_LOCATION = "CCCCE064-2432-42C2-9B06-8B8C4A266589";

        public const string SHOW_IMAGES = "DDDD8A11-EC71-4482-976E-1306C8464FA4";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.SelectionPropertiesConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(SHOW_TAGS, Strings.SelectionPropertiesConfiguration_ShowTags).WithValue(true))
                .WithElement(new BooleanConfigurationElement(SHOW_PROPERTIES, Strings.SelectionPropertiesConfiguration_ShowProperties).WithValue(true))
                .WithElement(new BooleanConfigurationElement(SHOW_REPLAYGAIN, Strings.SelectionPropertiesConfiguration_ShowReplayGain).WithValue(false))
                .WithElement(new BooleanConfigurationElement(SHOW_LOCATION, Strings.SelectionPropertiesConfiguration_ShowLocation).WithValue(true))
                .WithElement(new BooleanConfigurationElement(SHOW_IMAGES, Strings.SelectionPropertiesConfiguration_ShowImages).WithValue(true));
        }
    }
}
