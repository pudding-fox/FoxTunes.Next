using System;

namespace FoxTunes.Interfaces
{
    public interface IOutputEqualizerBand : IBaseComponent
    {
        int Position { get; }

        float MinCenter { get; }

        float MaxCenter { get; }

        float Center { get; set; }

        event EventHandler CenterChanged;

        float MinWidth { get; }

        float MaxWidth { get; }

        float Width { get; set; }

        event EventHandler WidthChanged;

        float MinValue { get; }

        float MaxValue { get; }

        float Value { get; set; }

        event EventHandler ValueChanged;
    }
}
