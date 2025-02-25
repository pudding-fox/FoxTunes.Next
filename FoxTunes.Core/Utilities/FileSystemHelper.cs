using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FoxTunes
{
    public static class FileSystemHelper
    {
        const int CACHE_SIZE = 128;

        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static HashSet<string> IgnoredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FoxTunes.Launcher.exe",
            "FoxTunes.Launcher.x86.exe",
            //TODO: Coupling to some other random component?
            "DataStore",
            "x86",
            "x64",
            //TODO: Translations should be better managed.
            "fr",
            //TODO: Coupling to some other random component?
            "encoders",
            //TODO: Coupling to some other random component?
            "Sox"
        };

        static FileSystemHelper()
        {
            Store = new Cache(CACHE_SIZE);
        }

        public static Cache Store { get; private set; }

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (searchOption.HasFlag(SearchOption.UseSystemCache))
            {
                //TODO: Warning: Buffering a potentially large sequence.
                return Store.GetOrAdd(path, searchPattern, searchOption, () => EnumerateFilesCore(path, searchPattern, searchOption).ToArray());
            }
            return EnumerateFilesCore(path, searchPattern, searchOption);
        }

        private static IEnumerable<string> EnumerateFilesCore(string path, string searchPattern, SearchOption searchOption)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                if (searchOption.HasFlag(SearchOption.Recursive))
                {
                    try
                    {
                        var directoryNames = Directory.EnumerateDirectories(path, "*", global::System.IO.SearchOption.TopDirectoryOnly);
                        if (searchOption.HasFlag(SearchOption.UseSystemExclusions))
                        {
                            directoryNames = WithSystemExclusions(directoryNames);
                        }
                        if (searchOption.HasFlag(SearchOption.Sort))
                        {
                            //The results are already sorted (if using NTFS)
                            //The underlying API is https://docs.microsoft.com/en-gb/windows/win32/api/fileapi/nf-fileapi-findnextfilea
                            //.NET doesn't specify any order though so here we are..
                            directoryNames = directoryNames.OrderBy();
                        }
                        queue.EnqueueRange(directoryNames);
                    }
                    catch
                    {
                        continue;
                    }
                }
                var fileNames = default(IEnumerable<string>);
                try
                {
                    fileNames = Directory.EnumerateFiles(path, searchPattern, global::System.IO.SearchOption.TopDirectoryOnly);
                    if (searchOption.HasFlag(SearchOption.UseSystemExclusions))
                    {
                        fileNames = WithSystemExclusions(fileNames);
                    }
                    if (searchOption.HasFlag(SearchOption.Sort))
                    {
                        //The results are already sorted (if using NTFS)
                        //The underlying API is https://docs.microsoft.com/en-gb/windows/win32/api/fileapi/nf-fileapi-findnextfilea
                        //.NET doesn't specify any order though so here we are..
                        fileNames = fileNames.OrderBy();
                    }
                }
                catch
                {
                    continue;
                }
                foreach (var fileName in fileNames)
                {
                    yield return fileName;
                }
            }
        }

        private static IEnumerable<string> WithSystemExclusions(IEnumerable<string> paths)
        {
            return paths.Where(path =>
            {
                if (IgnoredNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }).ToArray();
        }

        public static bool IsLocalPath(string fileName)
        {
            try
            {
                if (Publication.IsPortable)
                {
                    fileName = Path.GetFullPath(fileName);
                }
                var uri = default(Uri);
                if (!Uri.TryCreate(fileName, UriKind.Absolute, out uri))
                {
                    return false;
                }
                if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    //Normal path.
                    return true;
                }
                //Some kind of abstraction.
                return false;
            }
            catch (Exception e)
            {
                Logger.Write(typeof(FileSystemHelper), LogLevel.Warn, "Failed to determine whether \"{0}\" is a local path: {1}", fileName, e.Message);
                return false;
            }
        }

        public static bool TryGetRelativePath(string path1, string path2, out string path)
        {
            path = GetRelativePath(path1, path2);
            return !string.IsNullOrEmpty(path);
        }

        public static string GetRelativePath(string path1, string path2)
        {
            path1 = Path.GetFullPath(path1);
            path2 = Path.GetFullPath(path2);

            if (string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase))
            {
                return ".";
            }

            var parts1 = path1.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parts2 = path2.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var builder = new StringBuilder();
            builder.Append(".");

            for (int a = 0, b = Math.Max(parts1.Length, parts2.Length); a < b; a++)
            {
                if (a >= parts1.Length)
                {
                    builder.Append(Path.DirectorySeparatorChar);
                    builder.Append(parts2[a]);
                }
                else if (a >= parts2.Length)
                {
                    builder.Append(Path.DirectorySeparatorChar);
                    builder.Append(parts1[a]);
                }
                else if (!string.Equals(parts1[a], parts2[a], StringComparison.OrdinalIgnoreCase))
                {
                    //Paths cannot be relative, different locations.
                    return null;
                }
            }

            return builder.ToString();
        }

        public static string GetAbsolutePath(string path1, string path2)
        {
            var uri = new Uri(path2, UriKind.RelativeOrAbsolute);
            if (uri.IsAbsoluteUri)
            {
                return path2;
            }
            return Path.Combine(path1, path2.TrimStart('.', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        public class Cache
        {
            public Cache(int capacity)
            {
                this.Store = new CappedDictionary<Key, IEnumerable<string>>(capacity);
            }

            public CappedDictionary<Key, IEnumerable<string>> Store { get; private set; }

            public IEnumerable<string> GetOrAdd(string path, string searchPattern, SearchOption searchOption, Func<IEnumerable<string>> factory)
            {
                var key = new Key(path, searchPattern, searchOption);
                return this.Store.GetOrAdd(key, factory);
            }

            public class Key : IEquatable<Key>
            {
                public Key(string path, string searchPattern, SearchOption searchOption)
                {
                    this.Path = path;
                    this.SearchPattern = searchPattern;
                    this.SearchOption = searchOption;
                }

                public string Path { get; private set; }

                public string SearchPattern { get; private set; }

                public SearchOption SearchOption { get; private set; }

                public virtual bool Equals(Key other)
                {
                    if (other == null)
                    {
                        return false;
                    }
                    if (object.ReferenceEquals(this, other))
                    {
                        return true;
                    }
                    if (!string.Equals(this.Path, other.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    if (!string.Equals(this.SearchPattern, other.SearchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    if (this.SearchOption != other.SearchOption)
                    {
                        return false;
                    }
                    return true;
                }

                public override bool Equals(object obj)
                {
                    return this.Equals(obj as Key);
                }

                public override int GetHashCode()
                {
                    var hashCode = default(int);
                    unchecked
                    {
                        if (!string.IsNullOrEmpty(this.Path))
                        {
                            hashCode += this.Path.ToLower().GetHashCode();
                        }
                        if (!string.IsNullOrEmpty(this.SearchPattern))
                        {
                            hashCode += this.SearchPattern.ToLower().GetHashCode();
                        }
                        hashCode += this.SearchOption.GetHashCode();
                    }
                    return hashCode;
                }

                public static bool operator ==(Key a, Key b)
                {
                    if ((object)a == null && (object)b == null)
                    {
                        return true;
                    }
                    if ((object)a == null || (object)b == null)
                    {
                        return false;
                    }
                    if (object.ReferenceEquals((object)a, (object)b))
                    {
                        return true;
                    }
                    return a.Equals(b);
                }

                public static bool operator !=(Key a, Key b)
                {
                    return !(a == b);
                }
            }
        }

        [Flags]
        public enum SearchOption : byte
        {
            None = 0,
            Recursive = 1,
            UseSystemExclusions = 2,
            UseSystemCache = 4,
            Sort = 8
        }
    }
}
