using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface ILibraryManager : IStandardManager, IFileActionHandler
    {
        LibraryManagerState State { get; }

        LibraryHierarchy SelectedHierarchy { get; set; }

        event EventHandler SelectedHierarchyChanged;

        LibraryHierarchyNode SelectedItem { get; set; }

        event EventHandler SelectedItemChanged;

        Task Add(IEnumerable<string> paths);

        Task Rescan(bool force);

        Task SetStatus(LibraryItemStatus status);

        Task SetStatus(IEnumerable<LibraryItem> items, LibraryItemStatus status);

        Task Clear(LibraryItemStatus? status);
    }

    [Flags]
    public enum LibraryManagerState : byte
    {
        None = 0,
        Updating = 1
    }
}
