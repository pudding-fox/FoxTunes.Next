using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class LibraryRoot : PersistableComponent
    {
        public string DirectoryName { get; set; }

        public static IEnumerable<string> Normalize(IEnumerable<string> currentPaths, IEnumerable<string> newPaths)
        {
            var paths = currentPaths.Concat(newPaths).ToList();
            for (var a = paths.Count - 1; a >= 0; a--)
            {
                for (var b = 0; b < paths.Count; b++)
                {
                    if (a == b)
                    {
                        continue;
                    }
                    if (string.Equals(paths[a], paths[b], StringComparison.OrdinalIgnoreCase))
                    {
                        paths.RemoveAt(a);
                        break;
                    }
                    if (paths[a].Length > paths[b].Length && paths[a].StartsWith(paths[b], StringComparison.OrdinalIgnoreCase))
                    {
                        paths.RemoveAt(a);
                        break;
                    }
                }
            }
            return paths;
        }
    }
}
