using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataCache : IStandardComponent
    {
        IEnumerable<MetaDataCacheKey> Keys { get; }

        MetaDataItem[] GetMetaDatas(MetaDataCacheKey key, Func<IEnumerable<MetaDataItem>> factory);

        Task<MetaDataItem[]> GetMetaDatas(MetaDataCacheKey key, Func<Task<IEnumerable<MetaDataItem>>> factory);

        void Evict(MetaDataCacheKey key);

        void Reset();
    }

    public abstract class MetaDataCacheKey : IEquatable<MetaDataCacheKey>
    {
        protected MetaDataCacheKey(string name, MetaDataItemType type, int limit, string filter)
        {
            this.Name = name;
            this.Type = type;
            this.Limit = limit;
            this.Filter = filter;
        }

        public string Name { get; private set; }

        public MetaDataItemType Type { get; private set; }

        public int Limit { get; private set; }

        public string Filter { get; private set; }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    hashCode += this.Name.ToLower().GetHashCode();
                }
                hashCode += this.Type.GetHashCode();
                hashCode += this.Limit.GetHashCode();
                if (!string.IsNullOrEmpty(this.Filter))
                {
                    hashCode += this.Filter.ToLower().GetHashCode();
                }
            }
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as MetaDataCacheKey);
        }

        public virtual bool Equals(MetaDataCacheKey other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (this.Type != other.Type)
            {
                return false;
            }
            if (this.Limit != other.Limit)
            {
                return false;
            }
            if (!string.Equals(this.Filter, other.Filter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static bool operator ==(MetaDataCacheKey a, MetaDataCacheKey b)
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

        public static bool operator !=(MetaDataCacheKey a, MetaDataCacheKey b)
        {
            return !(a == b);
        }
    }

    public class LibraryMetaDataCacheKey : MetaDataCacheKey
    {
        public LibraryMetaDataCacheKey(LibraryHierarchyNode libraryHierarchyNode, string name, MetaDataItemType type, int limit, string filter) : base(name, type, limit, filter)
        {
            this.LibraryHierarchyNode = libraryHierarchyNode;
        }

        public LibraryHierarchyNode LibraryHierarchyNode { get; private set; }

        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            unchecked
            {
                if (this.LibraryHierarchyNode != null)
                {
                    hashCode += this.LibraryHierarchyNode.GetHashCode();
                }
            }
            return hashCode;
        }

        public override bool Equals(MetaDataCacheKey other)
        {
            return this.Equals(other as LibraryMetaDataCacheKey);
        }

        public bool Equals(LibraryMetaDataCacheKey other)
        {
            if (!base.Equals(other))
            {
                return false;
            }
            if (!object.Equals(this.LibraryHierarchyNode, other.LibraryHierarchyNode))
            {
                return false;
            }
            return true;
        }
    }

    public class PlaylistMetaDataCacheKey : MetaDataCacheKey
    {
        public PlaylistMetaDataCacheKey(PlaylistItem[] playlistItems, string name, MetaDataItemType type, int limit, string filter) : base(name, type, limit, filter)
        {
            this.PlaylistItems = playlistItems;
        }

        public PlaylistItem[] PlaylistItems { get; private set; }

        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            unchecked
            {
                if (this.PlaylistItems != null)
                {
                    foreach (var playlistItem in this.PlaylistItems)
                    {
                        hashCode += playlistItem.GetHashCode();
                    }
                }
            }
            return hashCode;
        }

        public override bool Equals(MetaDataCacheKey other)
        {
            return this.Equals(other as PlaylistMetaDataCacheKey);
        }

        public bool Equals(PlaylistMetaDataCacheKey other)
        {
            if (!base.Equals(other))
            {
                return false;
            }
            if (!Enumerable.SequenceEqual(this.PlaylistItems, other.PlaylistItems))
            {
                return false;
            }
            return true;
        }
    }
}
