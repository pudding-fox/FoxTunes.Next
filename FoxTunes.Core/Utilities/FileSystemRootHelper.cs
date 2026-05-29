using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static class FileSystemRootHelper
    {
        public static IEnumerable<string> GetRoots(IEnumerable<string> paths)
        {
            var root = new Node();
            foreach (var path in paths)
            {
                var directory = Path.GetDirectoryName(path) ?? path;
                var drive = Path.GetPathRoot(directory);
                if (string.IsNullOrEmpty(drive))
                {
                    continue;
                }
                var remainder = directory.Substring(drive.Length);
                var parts = remainder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (!root.Children.TryGetValue(drive, out var node))
                {
                    node = new Node(drive);
                    root.Children.Add(drive, node);
                }
                foreach (var part in parts)
                {
                    if (!node.Children.TryGetValue(part, out var child))
                    {
                        child = new Node(part);
                        node.Children.Add(part, child);
                    }

                    node = child;
                }
                node.IsTerminal = true;
            }
            var results = new List<string>();
            foreach (var child in root.Children.Values)
            {
                GetRoots(child, child.Name, results);
            }
            return results;
        }

        private static void GetRoots(Node node, string path, List<string> results)
        {
            if (node.Children.Count <= 1)
            {
                var current = node;
                var currentPath = path;
                while (current.Children.Count == 1 && !current.IsTerminal)
                {
                    current = current.Children.Values.First();
                    currentPath = currentPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? currentPath + current.Name : Path.Combine(currentPath, current.Name);
                }
                results.Add(currentPath);
                return;
            }
            foreach (var child in node.Children.Values)
            {
                GetRoots(child, Path.Combine(path, child.Name), results);
            }
        }

        private class Node
        {
            public Node()
            {
                this.Children = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
            }

            public Node(string name) : this()
            {
                this.Name = name;
            }

            public string Name { get; private set; }

            public bool IsTerminal { get; set; }

            public IDictionary<string, Node> Children { get; private set; }
        }
    }
}