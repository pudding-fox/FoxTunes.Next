using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IHierarchical
    {
        IHierarchical Parent { get; }

        IEnumerable<IHierarchical> Children { get; }
    }
}