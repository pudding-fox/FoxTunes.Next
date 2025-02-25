using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace FoxTunes
{
    public static class EnhancedSpectrumConfiguration
    {
        public const string SECTION = VisualizationBehaviourConfiguration.SECTION;

        public const string BANDS_ELEMENT = "AABBF573-83D3-498E-BEF8-F1DB5A329B9D";

        public const string BANDS_10_OPTION = "AAAA058C-2C96-4540-9ABE-10A584A17CE4";

        public const string BANDS_20_OPTION = "BBBB2B6D-E6FE-43F1-8358-AEE0299F0F8E";

        public const string BANDS_40_OPTION = "EEEE9CF3-A711-45D7-BFAC-A72E769106B9";

        public const string BANDS_80_OPTION = "FFFF83BC-5871-491D-963E-3D12554FF4BE";

        public const string BANDS_160_OPTION = "GGGG5E28-CC67-43F2-8778-61570785C766";

        public const string BANDS_CUSTOM_OPTION = "HHHH3A75-6409-45A1-9348-3CDC04BB8025";

        public const string BANDS_CUSTOM_ELEMENT = "AABCE681-F05A-48DD-9C0F-FA5B9BB4A2A7";

        public const string PEAK_ELEMENT = "BBBBDCF0-8B24-4321-B7BE-74DADE59D4FA";

        public const string RMS_ELEMENT = "DDDEE2B6A-188E-4FF4-A277-37D140D49C45";

        public const string COLOR_PALETTE_ELEMENT = "EEEE907A-5812-42CD-9844-89362C96C6AF";

        public const string COLOR_PALETTE_PEAK = "PEAK";

        public const string COLOR_PALETTE_RMS = "RMS";

        public const string COLOR_PALETTE_VALUE = "VALUE";

        public const string COLOR_PALETTE_BACKGROUND = "BACKGROUND";

        public const string DURATION_ELEMENT = "FFFF965B-101C-4A09-9A9A-91BAB17575E6";

        public const int DURATION_MIN = 16;

        public const int DURATION_MAX = 64;

        public const int DURATION_DEFAULT = 32;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.EnhancedSpectrumConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(BANDS_ELEMENT, Strings.EnhancedSpectrumConfiguration_Bands).WithOptions(GetBandsOptions()))
                .WithElement(new TextConfigurationElement(BANDS_CUSTOM_ELEMENT, Strings.EnhancedSpectrumConfiguration_Bands_Custom, description: Strings.EnhancedSpectrumConfiguration_Bands_Custom_Description).WithValue("50 20000 320 S").DependsOn(SECTION, BANDS_ELEMENT, BANDS_CUSTOM_OPTION))
                .WithElement(new BooleanConfigurationElement(PEAK_ELEMENT, Strings.EnhancedSpectrumConfiguration_Peak).WithValue(true))
                .WithElement(new BooleanConfigurationElement(RMS_ELEMENT, Strings.EnhancedSpectrumConfiguration_Rms).WithValue(true))
                .WithElement(new TextConfigurationElement(COLOR_PALETTE_ELEMENT, Strings.EnhancedSpectrumConfiguration_ColorPalette, path: Strings.General_Advanced).WithValue(GetDefaultColorPalette()).WithFlags(ConfigurationElementFlags.MultiLine))
                .WithElement(new IntegerConfigurationElement(DURATION_ELEMENT, Strings.EnhancedSpectrumConfiguration_Duration).WithValue(DURATION_DEFAULT).WithValidationRule(new IntegerValidationRule(DURATION_MIN, DURATION_MAX)))
                .WithElement(new IntegerConfigurationElement(VisualizationBehaviourConfiguration.INTERVAL_ELEMENT, Strings.VisualizationBehaviourConfiguration_Interval, path: Strings.General_Advanced).WithValue(VisualizationBehaviourConfiguration.DEFAULT_INTERVAL).WithValidationRule(new IntegerValidationRule(VisualizationBehaviourConfiguration.MIN_INTERVAL, VisualizationBehaviourConfiguration.MAX_INTERVAL)))
                .WithElement(new SelectionConfigurationElement(VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT, Strings.VisualizationBehaviourConfiguration_FFTSize, path: Strings.General_Advanced).WithOptions(VisualizationBehaviourConfiguration.GetFFTOptions(VisualizationBehaviourConfiguration.FFT_4096_OPTION))
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetBandsOptions()
        {
            yield return new SelectionConfigurationOption(BANDS_10_OPTION, "10");
            yield return new SelectionConfigurationOption(BANDS_20_OPTION, "20");
            yield return new SelectionConfigurationOption(BANDS_40_OPTION, "40");
            yield return new SelectionConfigurationOption(BANDS_80_OPTION, "80").Default();
            yield return new SelectionConfigurationOption(BANDS_160_OPTION, "160");
            yield return new SelectionConfigurationOption(BANDS_CUSTOM_OPTION, "Custom");
        }

        public static int[] GetBands(SelectionConfigurationOption option, TextConfigurationElement custom)
        {
            switch (option.Id)
            {
                case BANDS_10_OPTION:
                    return GetBands(10);
                case BANDS_20_OPTION:
                    return GetBands(20);
                case BANDS_40_OPTION:
                    return GetBands(40);
                case BANDS_80_OPTION:
                    return GetBands(80);
                case BANDS_160_OPTION:
                    return GetBands(160);
                case BANDS_CUSTOM_OPTION:
                    var parts = custom.Value.Split(new[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4)
                    {
                        var min = default(int);
                        var max = default(int);
                        var count = default(int);
                        var easingFunction = default(EasingFunction);
                        if (int.TryParse(parts[0], out min) && int.TryParse(parts[1], out max) && int.TryParse(parts[2], out count) && Enum.TryParse(parts[3], out easingFunction))
                        {
                            return GetBands(min, max, count, easingFunction);
                        }
                    }
                    break;
            }
            return GetBands(160);
        }

        private static int[] GetBands(int count)
        {
            return GetBands(50, 20000, count, EasingFunction.S);
        }

        private static int[] GetBands(int min, int max, int count, EasingFunction easingFunction)
        {
            var sequence = Enumerable.Range(0, count)
                .Select(element => (float)element / count)
                .Select(element =>
                {
                    switch (easingFunction)
                    {
                        case EasingFunction.L:
                            return element;
                        case EasingFunction.S:
                        default:
                            return element * element;
                    }
                });
            var bands = sequence.Select(element => Convert.ToInt32(min + (element * (max - min))))
                .ToArray();
            return bands;
        }

        public static string GetDefaultColorPalette()
        {
            var builder = new StringBuilder();
            return builder.ToString();
        }

        public static IDictionary<string, Color[]> GetColorPalette(string value)
        {
            var palettes = value.ToNamedColorStops().ToDictionary(
                pair => string.IsNullOrEmpty(pair.Key) ? COLOR_PALETTE_VALUE : pair.Key,
                pair => pair.Value.ToGradient(),
                StringComparer.OrdinalIgnoreCase
            );
            return palettes;
        }

        public enum EasingFunction : byte
        {
            None,
            [Description("Linear")]
            L,
            [Description("Square")]
            S
        }
    }
}
