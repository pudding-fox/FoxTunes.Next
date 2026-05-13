using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Reflection;

namespace FoxTunes
{
    public class AssemblyResolver : IAssemblyResolver
    {
        const FileSystemHelper.SearchOption SEARCH_OPTIONS =
            FileSystemHelper.SearchOption.Recursive |
            FileSystemHelper.SearchOption.UseSystemCache |
            FileSystemHelper.SearchOption.UseSystemExclusions;

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location);
            }
        }

        private AssemblyResolver()
        {

        }

        public void Enable()
        {
            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        }

        public void Disable()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
        }

        protected virtual Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var fileName = default(string);
            if (this.TryResolve(Location, args.Name, false, out fileName))
            {
                return Assembly.LoadFrom(fileName);
            }
            return null;
        }

        protected virtual bool TryResolve(string directoryName, string name, bool tryLoad, out string result)
        {
            foreach (var fileName in FileSystemHelper.EnumerateFiles(directoryName, "*.dll", SEARCH_OPTIONS))
            {
                var assemblyName = AssemblyRegistry.Instance.GetAssemblyName(fileName);
                if (assemblyName == null)
                {
                    continue;
                }
                if (!name.StartsWith(string.Concat(assemblyName.Name, ","), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                result = fileName;
                return true;
            }
            if (tryLoad)
            {
                //I'm not sure why but some platforms end up here for framework assemblies
                //like System.Runtime. I'm not sure how else to get the location.
                try
                {
                    var assembly = Assembly.Load(name);
                    if (File.Exists(assembly.Location))
                    {
                        result = assembly.Location;
                        return true;
                    }
                }
                catch
                {
                    //Nothing to do.
                }
            }
            result = default(string);
            return false;
        }

        public static readonly IAssemblyResolver Instance = new AssemblyResolver();
    }

    public class AssemblyResolverException : Exception
    {
        public AssemblyResolverException(string message)
            : base(message)
        {

        }
    }
}
