using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes.Interfaces
{
    public interface IAssemblyRegistry
    {
        IEnumerable<Assembly> ReflectionAssemblies { get; }

        IEnumerable<Assembly> ExecutableAssemblies { get; }

        AssemblyName GetAssemblyName(string fileName);

        Assembly GetOrLoadReflectionAssembly(string fileName);

        Assembly GetOrLoadExecutableAssembly(string fileName);

        Type GetReflectionType(Type type);

        Type GetExecutableType(Type type);
    }
}
