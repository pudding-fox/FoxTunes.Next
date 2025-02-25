using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static LibraryHierarchyNode Find(this IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes, LibraryHierarchyNode libraryHierarchyNode)
        {
            //First look for the new instance by id.
            var result = libraryHierarchyNodes.FirstOrDefault(_libraryHierarchyNode => _libraryHierarchyNode.Id == libraryHierarchyNode.Id);
            //If nothing was found or the value is different then look for the new instance by value.
            if (result == null || !string.Equals(result.Value, libraryHierarchyNode.Value, StringComparison.OrdinalIgnoreCase))
            {
                result = libraryHierarchyNodes.FirstOrDefault(_libraryHierarchyNode => string.Equals(_libraryHierarchyNode.Value, libraryHierarchyNode.Value, StringComparison.OrdinalIgnoreCase));
            }
            return result;
        }
    }
}
