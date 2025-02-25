using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IComponentActivator
    {
        T Activate<T>(Type type) where T : IBaseComponent;

        IEnumerable<IBaseComponent> Activate(IEnumerable<Type> components);
    }
}
