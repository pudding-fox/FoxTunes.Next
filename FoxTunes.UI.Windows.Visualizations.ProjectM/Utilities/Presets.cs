using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static class Presets
    {
        static Presets()
        {
            FileNames = FileSystemHelper.EnumerateFiles(
                Path.Combine(Location, "Presets"), "*.milk",
                FileSystemHelper.SearchOption.Recursive
            ).ToList();
            FileNames.Shuffle();
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location);
            }
        }

        public static List<string> FileNames { get; private set; }

        public static int Index { get; private set; }

        public static string Previous()
        {
            if (Index > 0)
            {
                Index--;
                return FileNames[Index];
            }
            else
            {
                Index = FileNames.Count - 1;
                return FileNames[Index];
            }
        }

        public static string Next()
        {
            if (Index < FileNames.Count)
            {
                Index++;
                return FileNames[Index];
            }
            else
            {
                Index = 0;
                return FileNames[Index];
            }
        }
    }
}
