using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static IEnumerable<LibraryHierarchyNode> GetParents(this IEnumerable<IFileData> fileDatas)
        {
            return fileDatas.OfType<LibraryItem>().GetParents();
        }

        public static IEnumerable<LibraryHierarchyNode> GetParents(this IEnumerable<LibraryItem> libraryItems)
        {
            return libraryItems.SelectMany(
                libraryItem => (IEnumerable<LibraryHierarchyNode>)libraryItem.Parents ?? Enumerable.Empty<LibraryHierarchyNode>()
            ).Distinct().ToArray();
        }
    }
}
