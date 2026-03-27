using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace FoxTunes.Integration
{
    public static class Explorer
    {
        const string HTTP = "http";
        const string HTTPS = "https";
        const string EXPLORER = "explorer.exe";

        public static void Select(string fileName)
        {
            Select(new[] { fileName });
        }

        public static void Select(IEnumerable<string> fileNames)
        {
            //Prepare a map of folders to file listings.
            var paths = new Dictionary<string, IList<string>>();
            foreach (var fileName in fileNames)
            {
                var uri = default(Uri);
                //If the file is an absolute path with a http or https protocol, use explorer.exe to open the default web browser.
                if (Uri.TryCreate(fileName, UriKind.Absolute, out uri))
                {
                    if (string.Equals(uri.Scheme, HTTP, StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, HTTPS, StringComparison.OrdinalIgnoreCase))
                    {
                        var args = string.Format("\"{0}\"", fileName);
                        Process.Start(EXPLORER, args);
                        continue;
                    }
                }

                //It's a file or folder path.
                var directoryName = Path.GetDirectoryName(fileName);
                paths.GetOrAdd(directoryName, () => new List<string>()).Add(fileName);
            }

            if (paths.Any())
            {
                Select(paths);
            }
        }

        public static void Select(IDictionary<string, IList<string>> paths)
        {
            const int WARN_LIMIT = 5;
            if (paths.Keys.Count > WARN_LIMIT)
            {
                var userInterface = ComponentRegistry.Instance.GetComponent<IUserInterface>();
                if (!userInterface.Confirm(string.Format(Strings.Explorer_WarnLimit, paths.Keys.Count)))
                {
                    //User cancelled.
                    return;
                }
            }

            foreach (var pair in paths)
            {
                //Fetch the desktop shell interface.
                var shell = default(IShellFolder);
                SHGetDesktopFolder(out shell);

                //Parse the folder.
                var folderName = default(IntPtr);
                {
                    var pchEaten = default(uint);
                    var pdwAttributes = default(uint);
                    shell.ParseDisplayName(IntPtr.Zero, null, pair.Key, out pchEaten, out folderName, ref pdwAttributes);
                }

                //Parse each file.
                var fileNames = new List<IntPtr>();
                foreach (var path in pair.Value)
                {
                    var fileName = default(IntPtr);
                    var pchEaten = default(uint);
                    var pdwAttributes = default(uint);
                    shell.ParseDisplayName(IntPtr.Zero, null, path, out pchEaten, out fileName, ref pdwAttributes);
                    fileNames.Add(fileName);
                }

                //Open the folder and select the files.
                var dwFlags = default(uint);
                SHOpenFolderAndSelectItems(folderName, (uint)fileNames.Count, fileNames.ToArray(), dwFlags);

                //Cleanup.
                foreach (var fileName in fileNames)
                {
                    ILFree(fileName);
                }
                ILFree(folderName);
            }
        }

        [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComConversionLoss]
        private interface IShellFolder
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ParseDisplayName(IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In, MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, [Out] out uint pchEaten, [Out] out IntPtr ppidl, [In, Out] ref uint pdwAttributes);
        }

        [DllImport("shell32.dll", EntryPoint = "SHGetDesktopFolder", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHGetDesktopFolder([MarshalAs(UnmanagedType.Interface)] out IShellFolder ppshf);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll")]
        public static extern void ILFree([In] IntPtr pidl);
    }
}
