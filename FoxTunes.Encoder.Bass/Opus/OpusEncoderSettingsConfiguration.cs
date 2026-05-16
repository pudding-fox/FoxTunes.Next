using System.Collections.Generic;

namespace FoxTunes
{
    public static class OpusEncoderSettingsConfiguration
    {
        public const string SECTION = BassEncoderBehaviourConfiguration.SECTION;

        public const string ENABLED = BassEncoderBehaviourConfiguration.ENABLED;

        public const string BITRATE = "AAAAD43D-6AC7-43A2-86DA-EA6323BD88B9";

        public const int MIN_BITRATE = 6;

        public const int MAX_BITRATE = 512;

        public const int DEFAULT_BITRATE = 256;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections(IBassEncoderSettings settings)
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new IntegerConfigurationElement(BITRATE, "Bitrate", path: settings.Name)
                    .WithValue(DEFAULT_BITRATE)
                    .WithValidationRule(new IntegerValidationRule(MIN_BITRATE, MAX_BITRATE))
                    .DependsOn(SECTION, ENABLED)
            );
        }
    }
}
