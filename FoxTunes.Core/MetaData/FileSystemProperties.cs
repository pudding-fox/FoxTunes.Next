using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoxTunes
{
    public static class FileSystemProperties
    {
        public const string FileName = "FileName";
        public const string DirectoryName = "DirectoryName";
        public const string FileExtension = "FileExtension";
        public const string FileSize = "FileSize";
        public const string FileCreationTime = "FileCreationTime";
        public const string FileModificationTime = "FileModificationTime";

        public static IDictionary<string, string> Lookup = GetLookup();

        private static IDictionary<string, string> GetLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in typeof(FileSystemProperties).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var name = field.GetValue(null) as string;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                lookup.Add(name, name);
            }
            return lookup;
        }
    }
}
