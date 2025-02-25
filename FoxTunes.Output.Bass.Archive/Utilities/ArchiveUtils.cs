using ManagedBass.ZipStream;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public static class ArchiveUtils
    {
        public const string SCHEME = "archive";

        private static readonly Lazy<string[]> _Extensions = new Lazy<string[]>(() =>
        {
            var instance = default(IntPtr);
            var extensions = new List<string>();
            if (Archive.Create(out instance))
            {
                try
                {
                    var count = default(int);
                    if (Archive.GetFormatCount(instance, out count))
                    {
                        for (var a = 0; a < count; a++)
                        {
                            var format = default(Archive.ArchiveFormat);
                            if (Archive.GetFormat(instance, out format, a))
                            {
                                if (!string.IsNullOrEmpty(format.extensions))
                                {
                                    foreach (var extension in format.extensions.Split(','))
                                    {
                                        extensions.Add(extension.Trim());
                                    }
                                }
                            }
                            else
                            {
                                //TODO: Warn.
                            }
                        }
                    }
                    else
                    {
                        //TODO: Warn.
                    }
                }
                finally
                {
                    Archive.Release(instance);
                }
            }
            else
            {
                //TODO: Warn.
            }
            return extensions.ToArray();
        });

        public static string[] Extensions
        {
            get
            {
                return _Extensions.Value;
            }
        }

        public static bool GetEntryIndex(string fileName, string entryName, out int index)
        {
            var archive = default(IntPtr);
            if (Archive.Create(out archive))
            {
                try
                {
                    if (Archive.Open(archive, fileName))
                    {
                        return GetEntryIndex(archive, entryName, out index);
                    }
                    else
                    {
                        //TODO: Warn.
                    }
                }
                finally
                {
                    Archive.Release(archive);
                }
            }
            else
            {
                //TODO: Warn.
            }
            index = -1;
            return false;
        }

        public static bool GetEntryIndex(IntPtr archive, string entryName, out int index)
        {
            var count = default(int);
            if (Archive.GetEntryCount(archive, out count))
            {
                for (index = 0; index < count; index++)
                {
                    var entry = default(Archive.ArchiveEntry);
                    if (Archive.GetEntry(archive, out entry, index))
                    {
                        if (string.Equals(entry.path, entryName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //TODO: Warn.
                    }
                }
            }
            else
            {
                //TODO: Warn.
            }
            index = -1;
            return false;
        }

        public static string CreateUrl(string fileName)
        {
            return string.Format(
                "{0}://{1}",
                SCHEME,
                fileName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            );
        }

        public static string CreateUrl(string fileName, string entryName)
        {
            return string.Format(
                "{0}://{1}?entry={2}",
                SCHEME,
                fileName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Uri.EscapeDataString(
                    entryName
                ).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            );
        }

        public static bool ParseUrl(string url, out string fileName, out string entryName)
        {
            return ParseUrl(new Uri(url), out fileName, out entryName);
        }

        public static bool ParseUrl(Uri url, out string fileName, out string entryName)
        {
            fileName = default(string);
            entryName = default(string);
            if (!string.Equals(url.Scheme, SCHEME, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            fileName = Uri.UnescapeDataString(
                url.AbsolutePath
            ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            entryName = Uri.UnescapeDataString(
                url.GetQueryParameter("entry")
            ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return true;
        }
    }
}
