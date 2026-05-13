using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes.Interfaces
{
    public interface IAssemblyRegistry
    {
        IEnumerable<Assembly> Assemblies { get; }

        AssemblyName GetAssemblyName(string fileName);

        Assembly GetOrLoadAssembly(string fileName);
    }
}
