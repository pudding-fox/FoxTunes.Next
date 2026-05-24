using System;
using System.IO;
using System.Reflection;

namespace FoxTunes
{
    public static class Program
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(Program).Assembly.Location);
            }
        }

        [STAThread]
        public static int Main(string[] args)
        {
            global::System.Diagnostics.Debugger.Launch();
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            try
            {
                MoodBarHost.Init();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            }
            //TODO: Bad .Result
            return MoodBarHost.Create().GetAwaiter().GetResult();
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var fileName = Path.Combine(Location, "..", "FoxTunes.Core.dll");
            if (File.Exists(fileName))
            {
                var assemblyName = AssemblyName.GetAssemblyName(fileName);
                if (string.Equals(assemblyName.FullName, args.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return Assembly.LoadFrom(fileName);
                }
            }
            return null;
        }
    }
}
