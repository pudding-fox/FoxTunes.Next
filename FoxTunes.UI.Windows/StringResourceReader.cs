using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;

namespace FoxTunes
{
    public static class StringResourceReader
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static readonly ConcurrentDictionary<Assembly, ResourceManager> ResourceManagers = new ConcurrentDictionary<Assembly, ResourceManager>();

        public static string GetString(Type type, string name)
        {
            return GetString(type.Assembly, type, name);
        }

        public static string GetString(Assembly assembly, Type type, string name)
        {
            var resourceManager = GetResourceManager(assembly);
            if (resourceManager == null)
            {
                Logger.Write(typeof(StringResourceReader), LogLevel.Warn, "Failed to determine ResourceManager for type \"{0}\".", type.FullName);
                return null;
            }
            var result = resourceManager.GetString(string.Format("{0}.{1}", type.Name, name));
            //TODO: We currently have a lot of missing Description strings so don't bother warning for now.
            //if (string.IsNullOrEmpty(result))
            //{
            //    Logger.Write(typeof(StringResourceReader), LogLevel.Warn, "Failed to locate resource string {0}.{1}.", type.Name, name);
            //}
            return result;
        }

        public static ResourceManager GetResourceManager(Assembly assembly)
        {
            return ResourceManagers.GetOrAdd(assembly, () =>
            {
                try
                {
                    var type = assembly.GetType(typeof(Strings).FullName);
                    if (type == null)
                    {
                        return null;
                    }
                    if (type.Assembly.ReflectionOnly)
                    {
                        type = AssemblyRegistry.Instance.GetExecutableType(type);
                    }
                    var property = type.GetProperty(nameof(Strings.ResourceManager), BindingFlags.Static | BindingFlags.NonPublic);
                    if (property == null)
                    {
                        return null;
                    }
                    return property.GetValue(null, null) as ResourceManager;
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(StringResourceReader), LogLevel.Error, "Failed to locate Strings ResourceManager in assembly \"{0}\": {1}", assembly.FullName, e.Message);
                    return null;
                }
            });
        }
    }
}
