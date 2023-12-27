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

        public void EnableExecution()
        {
            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        }

        public void EnableReflectionOnly()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this.OnReflectionOnlyAssemblyResolve;
        }

        public void DisableExecution()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
        }

        public void DisableReflectionOnly()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this.OnReflectionOnlyAssemblyResolve;
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

        protected virtual Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch
            {
                //Nothing to do.
            }
            var fileName = default(string);
            //TODO: We're setting the tryLoad parameter to true.
            //TODO: If the resolve fails for some reason a standard Assembly.Load will be attempted.
            //TODO: As we're trying to do a reflection only load this is obviously wrong.
            //TODO: It happens that the only libs that fall into this trap are framework assemblies so it's OK for now.
            if (this.TryResolve(Location, args.Name, true, out fileName))
            {
                var assembly = AssemblyRegistry.Instance.GetOrLoadReflectionAssembly(fileName);
                //Load immidiate references.
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly.ReflectionOnlyLoad(reference.FullName);
                    }
                    catch
                    {
                        //Nothing to do.
                    }
                }
                return assembly;
            }
            //Failed to resolve, perhaps somebody else will succeed.
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
                if (!string.Equals(assemblyName.FullName, name, StringComparison.OrdinalIgnoreCase))
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
