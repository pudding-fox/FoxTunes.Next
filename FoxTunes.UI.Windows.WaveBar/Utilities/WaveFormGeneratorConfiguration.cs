using System.Collections.Generic;

namespace FoxTunes
{
    public static class WaveFormGeneratorConfiguration
    {
        public const string SECTION = WaveFormStreamPositionConfiguration.SECTION;

        public const string RESOLUTION = "AAAACCC0-596C-489C-BD39-E74C0AE3697C";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.WaveFormStreamPositionConfiguration_Section)
                .WithElement(new IntegerConfigurationElement(RESOLUTION, Strings.WaveFormGeneratorConfiguration_Resolution, path: Strings.General_Advanced).WithValue(10).WithValidationRule(new IntegerValidationRule(1, 100))
            );
        }
    }
}
