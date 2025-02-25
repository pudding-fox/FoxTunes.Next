using System;

namespace FoxTunes
{
    public interface IColorPalette
    {
        string Id { get; }

        ColorPaletteRole Role { get; }

        string Name { get; }

        string Description { get; }

        string Value { get; }

        ColorPaletteFlags Flags { get; }
    }

    [Flags]
    public enum ColorPaletteRole : byte
    {
        None = 0,
        Visualization = 1,
        WaveForm = 2
    }

    [Flags]
    public enum ColorPaletteFlags : byte
    {
        None = 0,
        System = 1
    }
}
