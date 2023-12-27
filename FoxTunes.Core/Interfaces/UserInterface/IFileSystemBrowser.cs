using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IFileSystemBrowser : IBaseComponent //TODO: Technically this is a required interface (IStandardComponent).
    {
        void Select(string fileName);

        BrowseResult Browse(BrowseOptions options);
    }

    public class BrowseOptions
    {
        public BrowseOptions(string title, string path, IEnumerable<BrowseFilter> filters, BrowseFlags flags)
        {
            this.Title = title;
            this.Path = path;
            this.Filters = filters;
            this.Flags = flags;
        }

        public string Title { get; private set; }

        public string Path { get; private set; }

        public IEnumerable<BrowseFilter> Filters { get; private set; }

        public BrowseFlags Flags { get; private set; }
    }

    public class BrowseFilter
    {
        public BrowseFilter(string name, IEnumerable<string> extensions)
        {
            this.Name = name;
            this.Extensions = extensions;
        }

        public string Name { get; private set; }

        public IEnumerable<string> Extensions { get; private set; }
    }

    [Flags]
    public enum BrowseFlags : byte
    {
        None = 0,
        File = 1,
        Folder = 2,
        Multiselect = 4,
        Save = 8
    }

    public class BrowseResult
    {
        public BrowseResult(IEnumerable<string> paths, bool success)
        {
            this.Paths = paths;
            this.Success = success;
        }

        public IEnumerable<string> Paths { get; private set; }

        public bool Success { get; private set; }
    }
}
