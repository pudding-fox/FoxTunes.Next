using System;

namespace FoxTunes.Interfaces
{
    public interface IOutputEffect : IBaseComponent
    {
        bool Available { get; }

        event EventHandler AvailableChanged;

        bool Enabled { get; set; }

        event EventHandler EnabledChanged;
    }
}
