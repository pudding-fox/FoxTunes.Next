using System.Collections.Generic;

namespace FoxTunes
{
    public static class MoodBarGeneratorConfiguration
    {
        public const string SECTION = MoodBarStreamPositionConfiguration.SECTION;

        public const string TINT = "BBBBD94A-58F5-4AA8-8089-AE237BA8A8F0";

        public const string SAVE = "CCCC5B3E-6963-47B1-B18A-8CC75D7FDDEF";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MoodBarStreamPositionConfiguration_Section)
                .WithElement(new IntegerConfigurationElement(TINT, Strings.MoodBarGeneratorConfiguration_Tint, path: Strings.General_Advanced).WithValue(0).WithValidationRule(new IntegerValidationRule(0, 360)))
                .WithElement(new BooleanConfigurationElement(SAVE, Strings.MoodBarGeneratorConfiguration_Save, path: Strings.General_Advanced).WithValue(false)
            );
        }
    }
}
