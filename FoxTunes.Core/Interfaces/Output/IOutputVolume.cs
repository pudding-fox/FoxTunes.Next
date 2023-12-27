using System;

namespace FoxTunes.Interfaces
{
    public interface IOutputVolume : IOutputEffect
    {
        float Value { get; set; }

        event EventHandler ValueChanged;
    }
}
