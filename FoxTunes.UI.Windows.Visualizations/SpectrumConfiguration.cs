using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace FoxTunes
{
    public static class SpectrumConfiguration
    {
        public const string SECTION = VisualizationBehaviourConfiguration.SECTION;

        public const string BARS_ELEMENT = "AAAA0663-7CBF-4EE4-99C8-A0A096D4E876";

        public const string BARS_16_OPTION = "AAAADF84-DC4C-463E-9A76-D9D424890D91";

        public const string BARS_32_OPTION = "BBBBBA8B-CBA3-4800-B4BE-30D0C0758F7E";

        public const string BARS_64_OPTION = "CCCC9524-BC5A-48C0-8210-921B204307FC";

        //Had to these keys to keep the order of a context menu.... Looks like 128 came after 256 for some time.

        public const string BARS_128_OPTION = "DDDD058E-7B9C-4101-992A-709B87756991";

        public const string BARS_256_OPTION = "DDDD1FC1-592E-4EC6-9CCD-5182935AD12E";

        public const string BARS_512_OPTION = "DDDD2C33-9BCB-4A3A-ADD3-C26E2725CE31";

        public const string BARS_1024_OPTION = "DDDD3900-E91F-4FAB-A687-EA23065F4ECA";

        public const string PEAKS_ELEMENT = "DDDD7FCF-8A71-4367-8F48-4F8D8C89739C";

        public const string HOLD_ELEMENT = "EEEE64D9-FF15-49FB-BDF4-706958576FFC";

        public const int MIN_HOLD = 500;

        public const int MAX_HOLD = 5000;

        public const int DEFAULT_HOLD = 1000;

        public const string COLOR_PALETTE_ELEMENT = "FFFF957D-5AEA-4706-B6D0-9C9065E76132";

        public const string COLOR_PALETTE_PEAK = "PEAK";

        public const string COLOR_PALETTE_VALUE = "VALUE";

        public const string COLOR_PALETTE_BACKGROUND = "BACKGROUND";

        public const string CUT_OFF_ELEMENT = "GGGGA5E8-4D2D-4039-A03B-305679402052";

        public const int MIN_CUT_OFF = 0;

        public const int MAX_CUT_OFF = 20;

        public const int DEFAULT_CUT_OFF = 10;

        public const string PRE_AMP_ELEMENT = "HHHH2DBD-8CA9-4F41-9F61-1BC5F5D79545";

        public const int MIN_PRE_AMP = 0;

        public const int MAX_PRE_AMP = 20;

        public const int DEFAULT_PRE_AMP = 0;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.SpectrumConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(BARS_ELEMENT, Strings.SpectrumConfiguration_Bars).WithOptions(GetBarsOptions()))
                .WithElement(new BooleanConfigurationElement(PEAKS_ELEMENT, Strings.SpectrumConfiguration_Peaks).WithValue(true))
                .WithElement(new IntegerConfigurationElement(HOLD_ELEMENT, Strings.SpectrumConfiguration_Hold, path: Strings.General_Advanced).WithValue(DEFAULT_HOLD).WithValidationRule(new IntegerValidationRule(MIN_HOLD, MAX_HOLD)).DependsOn(SECTION, PEAKS_ELEMENT))
                .WithElement(new TextConfigurationElement(COLOR_PALETTE_ELEMENT, Strings.SpectrumConfiguration_ColorPalette, path: Strings.General_Advanced).WithValue(GetDefaultColorPalette()).WithFlags(ConfigurationElementFlags.MultiLine))
                .WithElement(new IntegerConfigurationElement(CUT_OFF_ELEMENT, Strings.SpectrumConfiguration_MaxFrequency, path: Strings.General_Advanced).WithValue(DEFAULT_CUT_OFF).WithValidationRule(new IntegerValidationRule(MIN_CUT_OFF, MAX_CUT_OFF)))
                .WithElement(new IntegerConfigurationElement(PRE_AMP_ELEMENT, Strings.SpectrumConfiguration_PreAmp, path: Strings.General_Advanced).WithValue(DEFAULT_PRE_AMP).WithValidationRule(new IntegerValidationRule(MIN_PRE_AMP, MAX_PRE_AMP)))
                .WithElement(new IntegerConfigurationElement(VisualizationBehaviourConfiguration.INTERVAL_ELEMENT, Strings.VisualizationBehaviourConfiguration_Interval, path: Strings.General_Advanced).WithValue(VisualizationBehaviourConfiguration.DEFAULT_INTERVAL).WithValidationRule(new IntegerValidationRule(VisualizationBehaviourConfiguration.MIN_INTERVAL, VisualizationBehaviourConfiguration.MAX_INTERVAL)))
                .WithElement(new SelectionConfigurationElement(VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT, Strings.VisualizationBehaviourConfiguration_FFTSize, path: Strings.General_Advanced).WithOptions(VisualizationBehaviourConfiguration.GetFFTOptions(VisualizationBehaviourConfiguration.FFT_512_OPTION))
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetBarsOptions()
        {
            yield return new SelectionConfigurationOption(BARS_16_OPTION, "16");
            yield return new SelectionConfigurationOption(BARS_32_OPTION, "32").Default();
            yield return new SelectionConfigurationOption(BARS_64_OPTION, "64");
            yield return new SelectionConfigurationOption(BARS_128_OPTION, "128");
            yield return new SelectionConfigurationOption(BARS_256_OPTION, "256");
            yield return new SelectionConfigurationOption(BARS_512_OPTION, "512");
            yield return new SelectionConfigurationOption(BARS_1024_OPTION, "1024");
        }

        public static int GetBars(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                case BARS_16_OPTION:
                    return 16;
                default:
                case BARS_32_OPTION:
                    return 32;
                case BARS_64_OPTION:
                    return 64;
                case BARS_128_OPTION:
                    return 128;
                case BARS_256_OPTION:
                    return 256;
                case BARS_512_OPTION:
                    return 512;
                case BARS_1024_OPTION:
                    return 1024;
            }
        }

        public static int GetWidth(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case BARS_16_OPTION:
                    return 160;
                case BARS_32_OPTION:
                    return 160;
                case BARS_64_OPTION:
                    return 192;
                case BARS_128_OPTION:
                    return 256;
                case BARS_256_OPTION:
                    return 256;
                case BARS_512_OPTION:
                    return 512;
                case BARS_1024_OPTION:
                    return 1024;
            }
        }

        public static int GetFFTSize(SelectionConfigurationOption bars, SelectionConfigurationOption fftSize)
        {
            var min = default(int);
            switch (bars.Id)
            {
                case BARS_16_OPTION:
                    min = 512;
                    break;
                default:
                case BARS_32_OPTION:
                    min = 512;
                    break;
                case BARS_64_OPTION:
                    min = 1024;
                    break;
                case BARS_128_OPTION:
                    min = 2048;
                    break;
                case BARS_256_OPTION:
                    min = 4096;
                    break;
                case BARS_512_OPTION:
                    min = 8192;
                    break;
                case BARS_1024_OPTION:
                    min = 16384;
                    break;
            }
            var max = VisualizationBehaviourConfiguration.GetFFTSize(fftSize);
            return Math.Max(min, max);
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
