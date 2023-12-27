using System;

namespace FoxTunes.Interfaces
{
    public interface IPersistableComponent : IBaseComponent, IEquatable<IPersistableComponent>
    {
        int Id { get; set; }
    }
}
