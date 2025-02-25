using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace FoxTunes
{
    public static class SpectrogramConfiguration
    {
        public const string SECTION = VisualizationBehaviourConfiguration.SECTION;

        public const string MODE_ELEMENT = "AAAA4D78-941A-42D3-A704-88E295D17324";

        public const string MODE_MONO_OPTION = "AAAABCAA-3545-4861-A4D0-F84475C09C6D";

        public const string MODE_SEPERATE_OPTION = "CCCC8D37-067D-4A43-8AE8-7AD000E3E176";

        public const string SCALE_ELEMENT = "BBBB3A79-02DD-4D56-A4F9-4641CF2BA1C8";

        public const string SCALE_LINEAR_OPTION = "AAAA4A52-A0E5-40CA-888B-3D6A29EC0683";

        public const string SCALE_LOGARITHMIC_OPTION = "BBBB5C59-8888-4734-A191-954709375BD0";

        public const string SMOOTHING_ELEMENT = "CCCC0B5F-C79E-410F-B302-704BF628DD57";

        public const int SMOOTHING_MIN = 0;

        public const int SMOOTHING_MAX = 5;

        public const int SMOOTHING_DEFAULT = 1;

        public const string COLOR_PALETTE_ELEMENT = "DDDDBAFC-0C99-4329-9DA2-C041E674597E";

        public const string COLOR_PALETTE_VALUE = "VALUE";

        public const string COLOR_PALETTE_BACKGROUND = "BACKGROUND";

        public const string HISTORY_ELEMENT = "EEEE2EBD-4483-4281-A4AD-E042E0367996";

        public const int HISTORY_MIN = 0;

        public const int HISTORY_MAX = 8192;

        public const int HISTORY_DEFAULT = 2048;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.SpectrogramConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(MODE_ELEMENT, Strings.SpectrogramConfiguration_Mode).WithOptions(GetModeOptions()))
                .WithElement(new SelectionConfigurationElement(SCALE_ELEMENT, Strings.SpectrogramConfiguration_Scale).WithOptions(GetScaleOptions()))
                .WithElement(new IntegerConfigurationElement(SMOOTHING_ELEMENT, Strings.SpectrogramConfiguration_Smoothing).WithValue(SMOOTHING_DEFAULT).WithValidationRule(new IntegerValidationRule(SMOOTHING_MIN, SMOOTHING_MAX)))
                .WithElement(new TextConfigurationElement(COLOR_PALETTE_ELEMENT, Strings.SpectrogramConfiguration_ColorPalette, path: Strings.General_Advanced).WithFlags(ConfigurationElementFlags.MultiLine))
                .WithElement(new IntegerConfigurationElement(HISTORY_ELEMENT, Strings.SpectrogramConfiguration_History, path: Strings.General_Advanced).WithValue(Publication.ReleaseType == ReleaseType.Default ? HISTORY_DEFAULT : HISTORY_MIN).WithValidationRule(new IntegerValidationRule(HISTORY_MIN, HISTORY_MAX)))
                .WithElement(new IntegerConfigurationElement(VisualizationBehaviourConfiguration.INTERVAL_ELEMENT, Strings.VisualizationBehaviourConfiguration_Interval, path: Strings.General_Advanced).WithValue(VisualizationBehaviourConfiguration.DEFAULT_INTERVAL).WithValidationRule(new IntegerValidationRule(VisualizationBehaviourConfiguration.MIN_INTERVAL, VisualizationBehaviourConfiguration.MAX_INTERVAL)))
                .WithElement(new SelectionConfigurationElement(VisualizationBehaviourConfiguration.FFT_SIZE_ELEMENT, Strings.VisualizationBehaviourConfiguration_FFTSize, path: Strings.General_Advanced).WithOptions(VisualizationBehaviourConfiguration.GetFFTOptions(VisualizationBehaviourConfiguration.FFT_512_OPTION))
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetModeOptions()
        {
            yield return new SelectionConfigurationOption(MODE_MONO_OPTION, Strings.SpectrogramConfiguration_Mode_Mono).Default();
            yield return new SelectionConfigurationOption(MODE_SEPERATE_OPTION, Strings.SpectrogramConfiguration_Mode_Seperate);
        }

        public static SpectrogramRendererMode GetMode(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case MODE_MONO_OPTION:
                    return SpectrogramRendererMode.Mono;
                case MODE_SEPERATE_OPTION:
                    return SpectrogramRendererMode.Seperate;
            }
        }

        private static IEnumerable<SelectionConfigurationOption> GetScaleOptions()
        {
            yield return new SelectionConfigurationOption(SCALE_LINEAR_OPTION, Strings.SpectrogramConfiguration_Scale_Linear).Default();
            yield return new SelectionConfigurationOption(SCALE_LOGARITHMIC_OPTION, Strings.SpectrogramConfiguration_Scale_Logarithmic);
        }

        public static SpectrogramRendererScale GetScale(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case SCALE_LINEAR_OPTION:
                    return SpectrogramRendererScale.Linear;
                case SCALE_LOGARITHMIC_OPTION:
                    return SpectrogramRendererScale.Logarithmic;
            }
        }

        public static string GetDefaultColorPalette()
        {
            var builder = new StringBuilder();
            builder.AppendLine("BLACK");
            builder.AppendLine("1000:WHITE");
            return builder.ToString();
        }

        public static Color[] GetColorPalette(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = GetDefaultColorPalette();
            }
            var palettes = value.ToNamedColorStops().ToDictionary(
                pair => string.IsNullOrEmpty(pair.Key) ? COLOR_PALETTE_VALUE : pair.Key,
                pair => pair.Value.ToGradient(),
                StringComparer.OrdinalIgnoreCase
            );
            var valueColors = default(Color[]);
            if (!palettes.TryGetValue(COLOR_PALETTE_VALUE, out valueColors))
            {
                valueColors = new[]
                {
                    Colors.White
                };
            }
            var backgroundColors = default(Color[]);
            if (!palettes.TryGetValue(COLOR_PALETTE_BACKGROUND, out backgroundColors))
            {
                backgroundColors = new[]
                {
                    Colors.Black
                };
            }
            if (valueColors.Length > 1)
            {
                return valueColors;
            }
            return backgroundColors.Concat(valueColors).ToArray().ToGradient(1000);
        }
    }
}
