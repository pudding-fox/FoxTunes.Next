using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public class Loader
    {
        public static readonly string FolderName = Path.Combine(Path.GetDirectoryName(typeof(Loader).Assembly.Location), Environment.Is64BitProcess ? "x64" : "x86");

        public static readonly string Extension = "dll";

        public static readonly ConcurrentDictionary<string, IntPtr> Handles = new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);

        public static bool Load(string fileName)
        {
            var module = default(IntPtr);
            return Load(fileName, out module);
        }

        public static bool Load(string fileName, out IntPtr module)
        {
            if (string.IsNullOrEmpty(Path.GetPathRoot(fileName)))
            {
                fileName = Path.Combine(FolderName, fileName);
            }
            module = Handles.GetOrAdd(fileName, key =>
            {
                var handle = GetModuleHandle(Path.GetFileName(fileName));
                if (IntPtr.Zero.Equals(handle))
                {
                    handle = LoadLibrary(fileName);
                }
                return handle;
            });
            if (IntPtr.Zero.Equals(module))
            {
                return false;
            }
            return true;
        }

        public static bool Free(string fileName)
        {
            var module = default(IntPtr);
            if (!Handles.TryRemove(fileName, out module))
            {
                return false;
            }
            if (IntPtr.Zero.Equals(module))
            {
                return false;
            }
            return FreeLibrary(module);
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}
