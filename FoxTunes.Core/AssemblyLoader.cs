using System;
using System.Reflection;

namespace FoxTunes
{
    public static class AssemblyLoader
    {
        static AssemblyLoader()
        {
            Loader = Assembly.LoadFrom;
        }

        public static Func<string, Assembly> Loader { get; private set; }
    }
}
