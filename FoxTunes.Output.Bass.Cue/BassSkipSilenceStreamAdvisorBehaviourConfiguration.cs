using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassSkipSilenceStreamAdvisorBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "RRRR223E-4396-495B-8600-5130CCEB81E0";

        public const string SENSITIVITY_ELEMENT = "SSSSA77F-83AF-476D-8A6C-6C75FB40242A";

        public const string SENSITIVITY_HIGH = "AAAA773C-DBB7-44B2-BFEF-C72926E0726F";

        public const string SENSITIVITY_MEDIUM = "BBBB0DD6-2EDB-4D6B-8F9D-691F2C45F8DB";

        public const string SENSITIVITY_LOW = "CCCC07F0-04DE-43E0-8D75-64711DA014C3";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, "Enabled", path: "Skip Silence").WithValue(false))
                .WithElement(new SelectionConfigurationElement(SENSITIVITY_ELEMENT, "Sensitivity", path: "Skip Silence")
                    .WithOptions(GetSensitivityOptions())
                    .DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT)
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetSensitivityOptions()
        {
            yield return new SelectionConfigurationOption(SENSITIVITY_HIGH, "High").Default();
            yield return new SelectionConfigurationOption(SENSITIVITY_MEDIUM, "Medium");
            yield return new SelectionConfigurationOption(SENSITIVITY_LOW, "Low");
        }

        public static int GetSensitivity(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case SENSITIVITY_HIGH:
                    //Digital silence.
                    return -1000;
                case SENSITIVITY_MEDIUM:
                    //-60dB
                    return -60;
                case SENSITIVITY_LOW:
                    //-40dB
                    return -40;
            }
        }
    }
}
