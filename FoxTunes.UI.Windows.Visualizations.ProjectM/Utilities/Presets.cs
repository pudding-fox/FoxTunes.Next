using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static class Presets
    {
        static Presets()
        {
            AllFileNames = FileSystemHelper.EnumerateFiles(
                Path.Combine(Location, "Presets"), "*.milk",
                FileSystemHelper.SearchOption.Recursive
            ).ToList();
            AllFileNames.Shuffle();
            FileNames = AllFileNames;
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(Presets).Assembly.Location);
            }
        }

        public static List<string> AllFileNames { get; private set; }

        public static List<string> FileNames { get; private set; }

        public static int Index { get; private set; }

        private static string _Search { get; set; }

        public static string Search
        {
            get
            {
                return _Search;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    FileNames = AllFileNames;
                }
                else
                {
                    FileNames = AllFileNames.Where(
                        fileName => fileName.Contains(value, true)
                    ).ToList();
                }
                _Search = value;
            }
        }

        public static string Previous()
        {
            if (Index > 0)
            {
                Index--;
                if (FileNames.Any())
                {
                    return FileNames[Index];
                }
            }
            else
            {
                Index = FileNames.Count - 1;
                if (FileNames.Any())
                {
                    return FileNames[Index];
                }
            }
            return string.Empty;
        }

        public static string Next()
        {
            if (Index < FileNames.Count - 1)
            {
                Index++;
                if (FileNames.Any())
                {
                    return FileNames[Index];
                }
            }
            else
            {
                Index = 0;
                if (FileNames.Any())
                {
                    return FileNames[Index];
                }
            }
            return string.Empty;
        }
    }
}
