using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace FoxTunes
{
    public static class PeakMeterConfiguration
    {
        public const string SECTION = VisualizationBehaviourConfiguration.SECTION;

        public const string PEAKS_ELEMENT = "AAAA3C5D-D292-4CD2-B258-A7652DBC77C6";

        public const string HOLD_ELEMENT = "BBBBD8E3-C38C-4B44-A080-DAE2E859D444";

        public const int MIN_HOLD = 500;

        public const int MAX_HOLD = 5000;

        public const int DEFAULT_HOLD = 1000;

        public const string COLOR_PALETTE = "EEEE1493-AF31-4450-A39B-396014866DDF";

        public const string COLOR_PALETTE_PEAK = "PEAK";

        public const string COLOR_PALETTE_VALUE = "VALUE";

        public const string COLOR_PALETTE_BACKGROUND = "BACKGROUND";

        public const string DURATION = "FFFF6E54-7313-45D9-9E30-6D7205725365";

        public const int DURATION_MIN = 4;

        public const int DURATION_MAX = 16;

        public const int DURATION_DEFAULT = 8;

        public const string MIN_FREQUENCY = "GGGG2086-53C8-4A38-98CD-73E756A190A9";

        public const int MIN_FREQUENCY_MIN = 0;

        public const int MIN_FREQUENCY_MAX = 20000;

        public const int MIN_FREQUENCY_DEFAULT = 5000;

        public const string MAX_FREQUENCY = "GGGG3915-611C-41A1-B4CB-36A118E5D222";

        public const int MAX_FREQUENCY_MIN = 0;

        public const int MAX_FREQUENCY_MAX = 20000;

        public const int MAX_FREQUENCY_DEFAULT = 10000;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.PeakMeterConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(PEAKS_ELEMENT, Strings.PeakMeterConfiguration_Peaks).WithValue(true))
                .WithElement(new IntegerConfigurationElement(HOLD_ELEMENT, Strings.PeakMeterConfiguration_Hold, path: Strings.General_Advanced).WithValue(DEFAULT_HOLD).WithValidationRule(new IntegerValidationRule(MIN_HOLD, MAX_HOLD)).DependsOn(SECTION, PEAKS_ELEMENT))
                .WithElement(new TextConfigurationElement(COLOR_PALETTE, Strings.PeakMeterConfiguration_ColorPalette, path: Strings.General_Advanced).WithValue(GetDefaultColorPalette()).WithFlags(ConfigurationElementFlags.MultiLine))
                .WithElement(new IntegerConfigurationElement(DURATION, Strings.PeakMeterConfiguration_Duration).WithValue(DURATION_DEFAULT).WithValidationRule(new IntegerValidationRule(DURATION_MIN, DURATION_MAX)))
                .WithElement(new IntegerConfigurationElement(MIN_FREQUENCY, Strings.PeakMeterConfiguration_MinFrequency, path: Strings.General_Advanced).WithValue(MIN_FREQUENCY_DEFAULT).WithValidationRule(new IntegerValidationRule(MIN_FREQUENCY_MIN, MIN_FREQUENCY_MAX, 10)))
                .WithElement(new IntegerConfigurationElement(MAX_FREQUENCY, Strings.PeakMeterConfiguration_MaxFrequency, path: Strings.General_Advanced).WithValue(MAX_FREQUENCY_DEFAULT).WithValidationRule(new IntegerValidationRule(MAX_FREQUENCY_MIN, MAX_FREQUENCY_MAX, 10)))
                .WithElement(new IntegerConfigurationElement(VisualizationBehaviourConfiguration.INTERVAL_ELEMENT, Strings.VisualizationBehaviourConfiguration_Interval, path: Strings.General_Advanced).WithValue(VisualizationBehaviourConfiguration.DEFAULT_INTERVAL).WithValidationRule(new IntegerValidationRule(VisualizationBehaviourConfiguration.MIN_INTERVAL, VisualizationBehaviourConfiguration.MAX_INTERVAL)))
                .WithElement(new SelectionConfigurationElement(VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT, Strings.VisualizationBehaviourConfiguration_FFTSize, path: Strings.General_Advanced).WithOptions(VisualizationBehaviourConfiguration.GetFFTOptions(VisualizationBehaviourConfiguration.FFT_512_OPTION))
            );
        }

        public static string GetDefaultColorPalette()
        {
            var builder = new StringBuilder();
            return builder.ToString();
        }

        public static IDictionary<string, Color[]> GetColorPalette(string value)
        {
            return value.ToNamedColorStops().ToDictionary(
                pair => string.IsNullOrEmpty(pair.Key) ? COLOR_PALETTE_VALUE : pair.Key,
                pair => pair.Value.ToGradient(),
                StringComparer.OrdinalIgnoreCase
            );
        }
    }
}
