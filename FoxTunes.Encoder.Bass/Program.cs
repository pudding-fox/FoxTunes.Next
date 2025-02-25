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
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            try
            {
                BassEncoderHost.Init();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            }
            return BassEncoderHost.Encode();
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
