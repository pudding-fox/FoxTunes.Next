using System.Collections.Generic;

namespace FoxTunes
{
    public class LibraryHierarchyCollection : ObservableCollection<LibraryHierarchy>
    {
        public LibraryHierarchyCollection(IEnumerable<LibraryHierarchy> libraryHierarchies) : base(libraryHierarchies)
        {

        }
    }
}
