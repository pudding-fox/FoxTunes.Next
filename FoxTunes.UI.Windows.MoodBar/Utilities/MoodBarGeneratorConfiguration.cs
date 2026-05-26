using System.Collections.Generic;

namespace FoxTunes
{
    public static class MoodBarGeneratorConfiguration
    {
        public const string SECTION = MoodBarStreamPositionConfiguration.SECTION;

        public const string TINT = "BBBBD94A-58F5-4AA8-8089-AE237BA8A8F0";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MoodBarStreamPositionConfiguration_Section)
                .WithElement(new IntegerConfigurationElement(TINT, Strings.MoodBarGeneratorConfiguration_Tint, path: Strings.General_Advanced).WithValue(0).WithValidationRule(new IntegerValidationRule(0, 360))
            );
        }
    }
}
