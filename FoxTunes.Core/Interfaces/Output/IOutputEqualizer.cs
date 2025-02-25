using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IOutputEqualizer : IOutputEffect
    {
        IEnumerable<IOutputEqualizerBand> Bands { get; }

        IEnumerable<string> Presets { get; }

        event EventHandler PresetsChanged;

        string Preset { get; set; }

        event EventHandler PresetChanged;

        void SavePreset(string name);
    }
}
