using System.Collections.Generic;

namespace FoxTunes
{
    public static class StaticImageConfiguration
    {
        public const string SECTION = "D98000CF-62FC-44CA-A6D2-51E013F2D331";

        public const string PATH = "AAAA1509-4808-419D-93BB-B8D914C60646";

        public const string INTERVAL = "BBBBA520-343A-40E9-AB94-82ED4970418B";

        public const int DEFAULT_INTERVAL = 60;

        public const int MIN_INTERVAL = 10;

        public const int MAX_INTERVAL = 300;

        public const string OPACITY = "AABBBC99-41C7-4563-BB7E-12EDAC59E57F";

        public const int DEFAULT_OPACITY = 100;

        public const int MIN_OPACITY = 1;

        public const int MAX_OPACITY = 100;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.StaticImageConfiguration_Section)
                .WithElement(new TextConfigurationElement(PATH, Strings.StaticImageConfiguration_Path)
                    .WithFlags(ConfigurationElementFlags.MultiLine))
                .WithElement(new IntegerConfigurationElement(INTERVAL, Strings.StaticImageConfiguration_Interval)
                    .WithValue(DEFAULT_INTERVAL)
                    .WithValidationRule(new IntegerValidationRule(MIN_INTERVAL, MAX_INTERVAL, 10)))
            .WithElement(new IntegerConfigurationElement(OPACITY, Strings.StaticImageConfiguration_Opacity)
                    .WithValue(DEFAULT_OPACITY)
                    .WithValidationRule(new IntegerValidationRule(MIN_OPACITY, MAX_OPACITY)));
        }
    }
}
