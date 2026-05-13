using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public class AssemblyRegistry : IAssemblyRegistry
    {
        private AssemblyRegistry()
        {
            this._AssemblyNames = new ConcurrentDictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);
            this._Assemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        }

        private ConcurrentDictionary<string, AssemblyName> _AssemblyNames { get; set; }

        private ConcurrentDictionary<string, Assembly> _Assemblies { get; set; }

        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                return this._Assemblies.Values;
            }
        }

        public AssemblyName GetAssemblyName(string fileName)
        {
            var assemblyName = default(AssemblyName);
            if (this._AssemblyNames.TryGetValue(fileName, out assemblyName))
            {
                return assemblyName;
            }
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(fileName);
            }
            catch
            {
                //Not a managed assembly.
            }
            this._AssemblyNames.TryAdd(fileName, assemblyName);
            return assemblyName;
        }

        public Assembly GetOrLoadAssembly(string fileName)
        {
            return _Assemblies.GetOrAdd(fileName, AssemblyLoader.Loader);
        }

        public static readonly IAssemblyRegistry Instance = new AssemblyRegistry();
    }

    public class AssemblyRegistryException : Exception
    {
        public AssemblyRegistryException(string message)
            : base(message)
        {

        }
    }
}
