using System;
using System.Reflection;

namespace FoxTunes
{
    public static class AssemblyLoader
    {
        static AssemblyLoader()
        {
            if (Publication.IsPortable)
            {
                ReflectionOnlyLoader = Assembly.ReflectionOnlyLoadFrom;
            }
            else
            {
                ReflectionOnlyLoader = Assembly.LoadFrom;
            }
            ExecutableLoader = Assembly.LoadFrom;
        }

        public static Func<string, Assembly> ReflectionOnlyLoader { get; private set; }

        public static Func<string, Assembly> ExecutableLoader { get; private set; }
    }
}
