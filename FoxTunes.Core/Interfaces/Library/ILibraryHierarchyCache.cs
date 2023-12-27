using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface ILibraryHierarchyCache : IStandardComponent
    {
        bool HasItems { get; }

        IEnumerable<LibraryHierarchyCacheKey> Keys { get; }

        LibraryHierarchy[] GetHierarchies(Func<IEnumerable<LibraryHierarchy>> factory);

        LibraryHierarchyNode[] GetNodes(LibraryHierarchyCacheKey key, Func<IEnumerable<LibraryHierarchyNode>> factory);

        LibraryItem[] GetItems(LibraryHierarchyCacheKey key, Func<IEnumerable<LibraryItem>> factory);

        void Evict(LibraryHierarchyCacheKey key);

        void Reset();
    }

    public class LibraryHierarchyCacheKey : IEquatable<LibraryHierarchyCacheKey>
    {
        public LibraryHierarchyCacheKey(LibraryHierarchy libraryHierarchy, LibraryHierarchyNode libraryHierarchyNode, string filter)
        {
            this.LibraryHierarchy = libraryHierarchy;
            this.LibraryHierarchyNode = libraryHierarchyNode;
            this.Filter = filter;
        }

        public LibraryHierarchy LibraryHierarchy { get; private set; }

        public LibraryHierarchyNode LibraryHierarchyNode { get; private set; }

        public string Filter { get; private set; }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                if (this.LibraryHierarchy != null)
                {
                    hashCode += this.LibraryHierarchy.GetHashCode();
                }
                if (this.LibraryHierarchyNode != null)
                {
                    hashCode += this.LibraryHierarchyNode.GetHashCode();
                }
                if (!string.IsNullOrEmpty(this.Filter))
                {
                    hashCode += this.Filter.ToLower().GetHashCode();
                }
            }
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as LibraryHierarchyCacheKey);
        }

        public bool Equals(LibraryHierarchyCacheKey other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!object.Equals(this.LibraryHierarchy, other.LibraryHierarchy))
            {
                return false;
            }
            if (!object.Equals(this.LibraryHierarchyNode, other.LibraryHierarchyNode))
            {
                return false;
            }
            if (!string.Equals(this.Filter, other.Filter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static bool operator ==(LibraryHierarchyCacheKey a, LibraryHierarchyCacheKey b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(LibraryHierarchyCacheKey a, LibraryHierarchyCacheKey b)
        {
            return !(a == b);
        }
    }
}
