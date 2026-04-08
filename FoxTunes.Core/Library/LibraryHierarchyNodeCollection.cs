using System.Collections.Generic;

namespace FoxTunes
{
    public class LibraryHierarchyNodeCollection : MergeableObservableCollection<LibraryHierarchyNode>
    {
        public LibraryHierarchyNodeCollection()
        {

        }

        public LibraryHierarchyNodeCollection(IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes) : base(libraryHierarchyNodes)
        {

        }

        protected override void Update(LibraryHierarchyNode source, LibraryHierarchyNode destination)
        {
            destination.Update(source);
            base.Update(source, destination);
        }
    }
}
