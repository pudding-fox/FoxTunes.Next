using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IHierarchyManager : IStandardManager, IDatabaseInitializer
    {
        HierarchyManagerState State { get; }

        Task Build();

        Task Build(IEnumerable<LibraryItem> libraryItems);

        Task Clear();
    }

    [Flags]
    public enum HierarchyManagerState : byte
    {
        None = 0,
        Updating = 1
    }
}
