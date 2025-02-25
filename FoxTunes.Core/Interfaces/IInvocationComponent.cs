using System;

namespace FoxTunes.Interfaces
{
    public interface IInvocationComponent : IBaseComponent
    {
        string Category { get; }

        string Id { get; }

        string Name { get; }

        string Description { get; }

        string Path { get; set; }

        object Source { get; set; }

        byte Attributes { get; set; }

        event EventHandler AttributesChanged;
    }
}
