using System.Collections.Generic;

namespace FoxTunes
{
    public static class ProjectMBehaviourConfiguration
    {
        public const string SECTION = "D8CB117D-005C-495C-8B23-A71589C0ED3D";

        public const string INTERVAL = "00009ECE-39D7-47F4-8CA1-A0A169A8BA03";

#if DEBUG
        public const int INTERVAL_DEFAULT = 15;
#else
        public const int INTERVAL_DEFAULT = 60;
#endif

        public const int INTERVAL_MIN = 0;

        public const int INTERVAL_MAX = 3600;

        public const string COMPLEXITY_LOW = "AAAA431F-0598-4A25-B6C7-9A50C3C9D238";

        public const string COMPLEXITY_MEDIUM = "BBBBBBD6-440F-472C-966C-CCBD8958D88F";

        public const string COMPLEXITY_HIGH = "CCCCCD16-3E56-41CF-BB3D-1CED380B37E6";

        public const string COMPLEXITY_EXTREME = "DDDD0D98-AB27-442D-8D3D-40A568098BB7";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.ProjectMBehaviourConfiguration_Section)
                .WithElement(new IntegerConfigurationElement(INTERVAL, Strings.ProjectMBehaviourConfiguration_Interval).WithValue(INTERVAL_DEFAULT).WithValidationRule(new IntegerValidationRule(INTERVAL_MIN, INTERVAL_MAX)))
                .WithElement(new BooleanConfigurationElement(COMPLEXITY_LOW, Strings.ProjectMBehaviourConfiguration_Complexity_Low).WithValue(true))
                .WithElement(new BooleanConfigurationElement(COMPLEXITY_MEDIUM, Strings.ProjectMBehaviourConfiguration_Complexity_Medium).WithValue(true))
                .WithElement(new BooleanConfigurationElement(COMPLEXITY_HIGH, Strings.ProjectMBehaviourConfiguration_Complexity_High).WithValue(false))
                .WithElement(new BooleanConfigurationElement(COMPLEXITY_EXTREME, Strings.ProjectMBehaviourConfiguration_Complexity_Extreme).WithValue(false));
        }
    }
}
