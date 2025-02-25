using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IComponentScanner
    {
        IEnumerable<Type> GetComponents();

        [Obsolete("This method has no caching, use it sparingly.")]
        IEnumerable<Type> GetComponents(Type interfaceType);

        IDictionary<string, IList<Type>> GetComponentsBySlot();
    }
}
