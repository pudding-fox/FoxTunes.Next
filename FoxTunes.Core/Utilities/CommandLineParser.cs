using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public static class CommandLineParser
    {
        public const string ADD = "--add";

        public const string PLAY = "--play";

        public static readonly Regex PATH = new Regex(
            @"((?:[a-zA-Z]\:(\\|\/)|file\:\/\/|\\\\|\.(\/|\\))([^\\\/\:\*\?\<\>\""\|]+(\\|\/){0,1})+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
         );

        public static bool TryParse(string command, out IEnumerable<string> paths, out OpenMode mode)
        {
            paths = Enumerable.Empty<string>();
            mode = OpenMode.Default;
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }
            else if (command.Contains(ADD, true))
            {
                mode = OpenMode.Add;
            }
            else if (command.Contains(PLAY, true))
            {
                mode = OpenMode.Play;
            }
            paths = GetPaths(command).ToArray();
            return paths.Any();
        }

        private static IEnumerable<string> GetPaths(string command)
        {
            var matches = PATH.Matches(command);
            for (var a = 0; a < matches.Count; a++)
            {
                var match = matches[a];
                if (!match.Success)
                {
                    continue;
                }
                var path = match.Value;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                path = path.Trim();
                if (Directory.Exists(path) || File.Exists(path))
                {
                    yield return path;
                }
            }
        }

        public enum OpenMode
        {
            None,
            Play,
            Add,
            Default = Play
        }
    }
}
