using System.Collections.Generic;

namespace FoxTunes
{
    public static class MoodBarGeneratorConfiguration
    {
        public const string SECTION = MoodBarStreamPositionConfiguration.SECTION;

        public const string RESOLUTION_ELEMENT = "AAAAB54B-E9E7-4800-AB3E-3F0E4324C24D";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MoodBarStreamPositionConfiguration_Section)
                .WithElement(new IntegerConfigurationElement(RESOLUTION_ELEMENT, Strings.MoodBarGeneratorConfiguration_Resolution, path: Strings.General_Advanced).WithValue(10).WithValidationRule(new IntegerValidationRule(1, 100))
            );
        }
    }
}
