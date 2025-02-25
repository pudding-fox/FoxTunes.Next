#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryHierarchyWriter : Disposable
    {
        const int CACHE_SIZE = 5120;

        private LibraryHierarchyWriter()
        {
            this.Store = new Cache(CACHE_SIZE);
        }

        public LibraryHierarchyWriter(IDatabaseComponent database, ITransactionSource transaction) : this()
        {
            this.AddCommand = CreateAddCommand(database, transaction);
            this.UpdateCommand = CreateUpdateCommand(database, transaction);
        }

        public Cache Store { get; private set; }

        public IDatabaseCommand AddCommand { get; private set; }

        public IDatabaseCommand UpdateCommand { get; private set; }

        public async Task<int> Write(LibraryHierarchy libraryHierarchy, int libraryItemId, int? parentId, string value, bool isLeaf)
        {
            var libraryHierarchyItemId = default(int);
            if (!isLeaf && this.Store.TryGetValue(libraryHierarchy.Id, parentId, value, out libraryHierarchyItemId))
            {
                this.UpdateCommand.Parameters["libraryHierarchyItemId"] = libraryHierarchyItemId;
                this.UpdateCommand.Parameters["libraryItemId"] = libraryItemId;
                await this.UpdateCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            else
            {
                this.AddCommand.Parameters["libraryHierarchyId"] = libraryHierarchy.Id;
                this.AddCommand.Parameters["libraryItemId"] = libraryItemId;
                this.AddCommand.Parameters["parentId"] = parentId;
                this.AddCommand.Parameters["value"] = value;
                this.AddCommand.Parameters["isLeaf"] = isLeaf;
                if (isLeaf)
                {
                    await this.AddCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                else
                {
                    this.Store.Add(
                        libraryHierarchy.Id,
                        parentId,
                        value,
                        libraryHierarchyItemId = Converter.ChangeType<int>(await this.AddCommand.ExecuteScalarAsync().ConfigureAwait(false))
                    );
                }
            }
            return libraryHierarchyItemId;
        }

        protected override void OnDisposing()
        {
            this.AddCommand.Dispose();
            this.UpdateCommand.Dispose();
            base.OnDisposing();
        }

        private static IDatabaseCommand CreateAddCommand(IDatabaseComponent database, ITransactionSource transaction)
        {
            return database.CreateCommand(
                database.Queries.AddLibraryHierarchyNode,
                DatabaseCommandFlags.NoCache,
                transaction
            );
        }

        private static IDatabaseCommand CreateUpdateCommand(IDatabaseComponent database, ITransactionSource transaction)
        {
            return database.CreateCommand(
                database.Queries.UpdateLibraryHierarchyNode,
                DatabaseCommandFlags.NoCache,
                transaction
            );
        }

        public class Cache
        {
            public Cache(int capacity)
            {
                this.Store = new CappedDictionary<Key, int>(capacity);
            }

            public CappedDictionary<Key, int> Store { get; private set; }

            public void Add(int libraryHierarchyId, int? parentId, string value, int libraryHierarchyItemId)
            {
                var key = new Key(libraryHierarchyId, parentId, value);
                this.Store.Add(key, libraryHierarchyItemId);
            }

            public bool TryGetValue(int libraryHierarchyId, int? parentId, string value, out int libraryHierarchyItemId)
            {
                var key = new Key(libraryHierarchyId, parentId, value);
                return this.Store.TryGetValue(key, out libraryHierarchyItemId);
            }

            public class Key : IEquatable<Key>
            {
                public Key(int libraryHierarchyId, int? parentId, string value)
                {
                    this.LibraryHierarchyId = libraryHierarchyId;
                    this.ParentId = parentId;
                    this.Value = value;
                }

                public int LibraryHierarchyId { get; private set; }

                public int? ParentId { get; private set; }

                public string Value { get; private set; }

                public virtual bool Equals(Key other)
                {
                    if (other == null)
                    {
                        return false;
                    }
                    if (object.ReferenceEquals(this, other))
                    {
                        return true;
                    }
                    if (this.LibraryHierarchyId != other.LibraryHierarchyId)
                    {
                        return false;
                    }
                    if (this.ParentId != other.ParentId)
                    {
                        return false;
                    }
                    if (!string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }

                public override bool Equals(object obj)
                {
                    return this.Equals(obj as Key);
                }

                public override int GetHashCode()
                {
                    var hashCode = default(int);
                    unchecked
                    {
                        hashCode += this.LibraryHierarchyId.GetHashCode();
                        hashCode += this.ParentId.GetHashCode();
                        if (!string.IsNullOrEmpty(this.Value))
                        {
                            hashCode += this.Value.ToLower().GetHashCode();
                        }
                    }
                    return hashCode;
                }

                public static bool operator ==(Key a, Key b)
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

                public static bool operator !=(Key a, Key b)
                {
                    return !(a == b);
                }
            }
        }
    }
}
