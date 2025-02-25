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
            this._ReflectionAssemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            this._ExecutableAssemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        }

        private ConcurrentDictionary<string, AssemblyName> _AssemblyNames { get; set; }

        private ConcurrentDictionary<string, Assembly> _ReflectionAssemblies { get; set; }

        public IEnumerable<Assembly> ReflectionAssemblies
        {
            get
            {
                return this._ReflectionAssemblies.Values;
            }
        }

        private ConcurrentDictionary<string, Assembly> _ExecutableAssemblies { get; set; }

        public IEnumerable<Assembly> ExecutableAssemblies
        {
            get
            {
                return this._ExecutableAssemblies.Values;
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

        public Assembly GetOrLoadReflectionAssembly(string fileName)
        {
            return this.GetOrLoadAssembly(this._ReflectionAssemblies, fileName, () => AssemblyLoader.ReflectionOnlyLoader(fileName));
        }

        public Assembly GetOrLoadExecutableAssembly(string fileName)
        {
            return this.GetOrLoadAssembly(this._ExecutableAssemblies, fileName, () => AssemblyLoader.ExecutableLoader(fileName));
        }

        private Assembly GetOrLoadAssembly(ConcurrentDictionary<string, Assembly> assemblies, string fileName, Func<Assembly> loader)
        {
            var assembly = default(Assembly);
            if (assemblies.TryGetValue(fileName, out assembly))
            {
                return assembly;
            }
            assembly = loader();
            if (!assemblies.TryAdd(fileName, assembly))
            {
                throw new AssemblyRegistryException(string.Format("Failed to cache assembly {0}.", fileName));
            }
            return assembly;
        }

        public Type GetReflectionType(Type type)
        {
            if (type.Assembly.ReflectionOnly)
            {
                return type;
            }
            var assembly = this.GetOrLoadReflectionAssembly(type.Assembly.Location);
            if (assembly == null)
            {
                throw new AssemblyRegistryException(string.Format("Failed to get reflection only assembly: {0}", type.Assembly.Location));
            }
            var reflection = assembly.GetType(type.FullName);
            if (reflection == null)
            {
                throw new AssemblyRegistryException(string.Format("Failed to get reflection only type: {0}", type.FullName));
            }
            return reflection;
        }

        public Type GetExecutableType(Type type)
        {
            if (!type.Assembly.ReflectionOnly)
            {
                return type;
            }
            var assembly = this.GetOrLoadExecutableAssembly(type.Assembly.Location);
            if (assembly == null)
            {
                throw new AssemblyRegistryException(string.Format("Failed to get executable assembly: {0}", type.Assembly.Location));
            }
            var reflection = assembly.GetType(type.FullName);
            if (reflection == null)
            {
                throw new AssemblyRegistryException(string.Format("Failed to get executable type: {0}", type.FullName));
            }
            return reflection;
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
