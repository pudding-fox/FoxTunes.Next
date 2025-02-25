using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassDsdBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string DSD_RATE_ELEMENT = "AAAAC170-6E90-4E55-9C14-8FCDAA39276B";

        public const string DSD_GAIN_ELEMENT = "BBBB4749-63C8-4866-9013-9AF01072CD2D";

        public const string DSD_MEMORY_ELEMENT = "CCCCA774-0E1D-4DA4-B03F-7063B4054A8F";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new SelectionConfigurationElement(DSD_RATE_ELEMENT, Strings.BassDsdBehaviourConfiguration_Rate, path: Strings.BassDsdBehaviourConfiguration_Path).WithOptions(GetRateOptions()))
                .WithElement(new IntegerConfigurationElement(DSD_GAIN_ELEMENT, Strings.BassDsdBehaviourConfiguration_Gain, path: Strings.BassDsdBehaviourConfiguration_Path).WithValue(6).WithValidationRule(new IntegerValidationRule(0, 10)))
                .WithElement(new BooleanConfigurationElement(DSD_MEMORY_ELEMENT, Strings.BassDsdBehaviourConfiguration_Memory, path: Strings.BassDsdBehaviourConfiguration_Path).WithValue(false)
            );
        }

        public static IEnumerable<SelectionConfigurationOption> GetRateOptions()
        {
            foreach (var rate in OutputRate.PCM)
            {
                var option = new SelectionConfigurationOption(rate.ToString(), rate.ToString());
                if (rate == OutputRate.PCM_88200)
                {
                    option.Default();
                }
                yield return option;
            }
        }

        public static int GetRate(SelectionConfigurationOption option)
        {
            var rate = default(int);
            if (int.TryParse(option.Id, out rate))
            {
                return rate;
            }
            return OutputRate.PCM_88200;
        }
    }
}
