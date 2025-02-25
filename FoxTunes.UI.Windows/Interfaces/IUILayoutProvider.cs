using System;

namespace FoxTunes.Interfaces
{
    public interface IUILayoutProvider : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        UIComponentBase Load(UILayoutTemplate template);

        event EventHandler Updated;

        UIComponentBase PresetSelector { get; }
    }

    public enum UILayoutTemplate : byte
    {
        None,
        Main
    }
}
