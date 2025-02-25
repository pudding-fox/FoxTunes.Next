using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public interface ITheme : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        Stream GetArtworkPlaceholder();

        IEnumerable<IColorPalette> ColorPalettes { get; }

        ThemeFlags Flags { get; }

        void Enable();

        void Disable();
    }

    [Flags]
    public enum ThemeFlags : byte
    {
        None = 0
    }
}
