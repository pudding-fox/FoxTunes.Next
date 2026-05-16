using System.Collections.Generic;

namespace FoxTunes
{
    public static class WavPackSettingsConfiguration
    {
        public const string SECTION = BassEncoderBehaviourConfiguration.SECTION;

        public const string ENABLED = BassEncoderBehaviourConfiguration.ENABLED;

        public const string DEPTH = "AAAAF597-9BC4-45AD-AEA6-AEB20E515247";

        public const string DEPTH_AUTO_OPTION = "BBBBDEFA-1E12-4F2B-82B2-D45CBDEBE2C9";

        public const string DEPTH_16_OPTION = "DDDDBF2B-CD12-4C5F-9708-105C85485177";

        public const string DEPTH_24_OPTION = "FFFF01748-BC1B-4C57-8F91-C723EE281164";

        public const string DEPTH_32_OPTION = "GGGG7F39-C56E-4F70-8567-40D293E59823";

        public const string COMPRESSION = "FFFF4CA0-28E0-4C63-B395-2774BC313229";

        public const string PROCESSING = "GGGG7D83-BA6A-4FA6-A3F5-786435C8146B";

        public const string HYBRID = "HHHH6694-AEE2-4623-8B4B-5928B37FB1B2";

        public const string BITRATE = "IIII00E7-2F19-46E4-A31E-8CBCFD50372A";

        public const string CORRECTION = "JJJJ78CE-A87C-4AC3-886D-3BBE6178BFEB";

        public const int DEFAULT_DEPTH = 16;

        public const int MIN_COMPRESSION = 0;

        public const int MAX_COMPRESSION = 3;

        public const int DEFAULT_COMPRESSION = 1;

        public const int MIN_PROCESSING = 0;

        public const int MAX_PROCESSING = 6;

        public const int DEFAULT_PROCESSING = 0;

        public const int MIN_BITRATE = 24;

        public const int MAX_BITRATE = 2048;

        public const int DEFAULT_BITRATE = 320;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections(IBassEncoderSettings settings)
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new SelectionConfigurationElement(DEPTH, "Depth", path: settings.Name)
                    .WithOptions(GetDepthOptions())
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new IntegerConfigurationElement(COMPRESSION, "Compression Level", path: settings.Name)
                    .WithValue(DEFAULT_COMPRESSION)
                    .WithValidationRule(new IntegerValidationRule(MIN_COMPRESSION, MAX_COMPRESSION))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new IntegerConfigurationElement(PROCESSING, "Additional Processing", path: settings.Name)
                    .WithValue(DEFAULT_PROCESSING)
                    .WithValidationRule(new IntegerValidationRule(MIN_PROCESSING, MAX_PROCESSING))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(HYBRID, "Hybrid Lossy", path: settings.Name)
                    .WithValue(false)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(CORRECTION, "Correction File", path: settings.Name)
                    .WithValue(false).DependsOn(SECTION, HYBRID)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new IntegerConfigurationElement(BITRATE, "Bitrate", path: settings.Name)
                    .WithValue(DEFAULT_BITRATE).DependsOn(SECTION, HYBRID)
                    .WithValidationRule(new IntegerValidationRule(MIN_BITRATE, MAX_BITRATE))
                    .DependsOn(SECTION, ENABLED)
                );
        }

        private static IEnumerable<SelectionConfigurationOption> GetDepthOptions()
        {
            yield return new SelectionConfigurationOption(DEPTH_AUTO_OPTION, "Auto");
            yield return new SelectionConfigurationOption(DEPTH_16_OPTION, "16bit");
            yield return new SelectionConfigurationOption(DEPTH_24_OPTION, "24bit");
            yield return new SelectionConfigurationOption(DEPTH_32_OPTION, "32bit");
        }

        public static int GetDepth(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                case DEPTH_AUTO_OPTION:
                    return BassEncoderSettings.DEPTH_AUTO;
                case DEPTH_16_OPTION:
                    return BassEncoderSettings.DEPTH_16;
                case DEPTH_24_OPTION:
                    return BassEncoderSettings.DEPTH_24;
                case DEPTH_32_OPTION:
                    return BassEncoderSettings.DEPTH_32;
                default:
                    return DEFAULT_DEPTH;
            }
        }
    }
}
