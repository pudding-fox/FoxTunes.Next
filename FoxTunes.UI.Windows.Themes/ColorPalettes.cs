using FoxDb;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static class ColorPalettes
    {
        public static IEnumerable<IColorPalette> Custom
        {
            get
            {
                return new[]
                {
                    new ThemeBase.ColorPalette(
                        "Custom_AAAA",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_MonoChrome,
                        string.Empty,
                        Resources.MonoChrome
                    ),
                    new ThemeBase.ColorPalette(
                        "Custom_BBBB",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_TrafficLight,
                        string.Empty,
                        Resources.TrafficLight
                    ),
                    new ThemeBase.ColorPalette(
                        "Custom_CCCC",
                        ColorPaletteRole.Visualization,
                        Strings.ColorPalette_RetroVibes,
                        string.Empty,
                        Resources.RetroVibes
                    ),
                    new ThemeBase.ColorPalette(
                        "Custom_DDDD",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_Audacity,
                        string.Empty,
                        Resources.Audacity
                    )
                };
            }
        }

        public static IEnumerable<IColorPalette> Light
        {
            get
            {
                return new[]
                {
                    new ThemeBase.ColorPalette(
                        "Light_AAAA",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_Default,
                        string.Empty,
                        Resources.Blue
                    ),
                    new ThemeBase.ColorPalette(
                        "Light_BBBB",
                        ColorPaletteRole.Visualization,
                        Strings.ColorPalette_Gradient,
                        string.Empty,
                        Resources.Transparent_Blue
                    ),
                }.Concat(Custom).ToArray();
            }
        }

        public static IEnumerable<IColorPalette> Dark
        {
            get
            {
                return new[]
                {
                    new ThemeBase.ColorPalette(
                        "Dark_AAAA",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_Default,
                        string.Empty,
                        Resources.White
                    ),
                    new ThemeBase.ColorPalette(
                        "Dark_BBBB",
                        ColorPaletteRole.Visualization,
                        Strings.ColorPalette_Gradient,
                        string.Empty,
                        Resources.Transparent_White
                    ),
                }.Concat(Custom).ToArray();
            }
        }

        public static IEnumerable<IColorPalette> Transparent
        {
            get
            {
                return new[]
                {
                    new ThemeBase.ColorPalette(
                        "Transparent_AAAA",
                        ColorPaletteRole.Visualization | ColorPaletteRole.WaveForm,
                        Strings.ColorPalette_Transparent,
                        string.Empty,
                        Resources.Transparent,
                        ColorPaletteFlags.System
                    ),
                }.Concat(Custom).ToArray();
            }
        }
    }
}
