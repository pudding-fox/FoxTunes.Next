using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassParametricEqualizerStreamComponentConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED = "AAAAF34F-A090-4AEE-BD65-128561960C92";

        public const string BANDWIDTH = "AAAB688D-9CA0-41B3-8E11-AC252DF03BE4";

        public const string BAND_32 = "BBBBC109-B99A-49C1-909D-16A862EFF344";

        public const string BAND_64 = "CCCCDD9E-7430-4123-B7CA-FDDD7680EF2C";

        public const string BAND_125 = "DDDD4934-28A4-4841-BC31-9BB895B13A14";

        public const string BAND_250 = "EEEE45E7-0B32-4E73-B394-2D510A34FB05";

        public const string BAND_500 = "FFFFD2A0-40A7-4772-9829-48D7C8CDCF52";

        public const string BAND_1000 = "GGGGEFB4-F65D-444F-A021-5B8F0B643AC5";

        public const string BAND_2000 = "HHHHB649-B83F-48D0-AF02-FA127FFC9F04";

        public const string BAND_4000 = "IIIIE3B6-1026-4505-8C77-294185F61452";

        public const string BAND_8000 = "JJJJAADF-5286-46BA-AC4F-80CFB1115215";

        public const string BAND_16000 = "KKKKD105-FC44-40F4-AF85-972CF7F328E9";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            var section = new ConfigurationSection(SECTION)
                .WithElement(
                    new BooleanConfigurationElement(ENABLED, Strings.BassParametricEqualizerStreamComponentConfiguration_Enabled, path: Strings.BassParametricEqualizerStreamComponentConfiguration_Path)
                        .WithValue(false))
                .WithElement(
                    new DoubleConfigurationElement(BANDWIDTH, Strings.BassParametricEqualizerStreamComponentConfiguration_Bandwidth, path: Strings.BassParametricEqualizerStreamComponentConfiguration_Path)
                        .WithValue(2.5)
                        .DependsOn(BassOutputConfiguration.SECTION, ENABLED)
                        .WithValidationRule(
                            new DoubleValidationRule(
                                PeakEQ.MIN_BANDWIDTH,
                                PeakEQ.MAX_BANDWIDTH,
                                0.1
                            )
                        ));

            foreach (var band in Bands)
            {
                section.WithElement(
                    new DoubleConfigurationElement(
                            band.Key,
                            band.Value < 1000 ? band.Value.ToString() + "Hz" : band.Value.ToString() + "kHz",
                            path: Strings.BassParametricEqualizerStreamComponentConfiguration_Path)
                        .WithValue(0)
                        .DependsOn(BassOutputConfiguration.SECTION, ENABLED)
                        .WithValidationRule(
                            new DoubleValidationRule(
                                PeakEQ.MIN_GAIN,
                                PeakEQ.MAX_GAIN
                            )
                        )
                );
            }

            yield return section;
        }

        public static IEnumerable<KeyValuePair<string, int>> Bands
        {
            get
            {
                yield return new KeyValuePair<string, int>(BAND_32, 32);
                yield return new KeyValuePair<string, int>(BAND_64, 64);
                yield return new KeyValuePair<string, int>(BAND_125, 125);
                yield return new KeyValuePair<string, int>(BAND_250, 250);
                yield return new KeyValuePair<string, int>(BAND_500, 500);
                yield return new KeyValuePair<string, int>(BAND_1000, 1000);
                yield return new KeyValuePair<string, int>(BAND_2000, 2000);
                yield return new KeyValuePair<string, int>(BAND_4000, 4000);
                yield return new KeyValuePair<string, int>(BAND_8000, 8000);
                yield return new KeyValuePair<string, int>(BAND_16000, 16000);
            }
        }
    }
}
