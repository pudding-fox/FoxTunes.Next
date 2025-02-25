using FoxDb.Interfaces;

namespace FoxTunes.Interfaces
{
    public interface IDatabaseTables : IBaseComponent, IInitializable
    {
        ITableConfig MetaDataItem { get; }

        ITableConfig PlaylistItem { get; }

        ITableConfig PlaylistColumn { get; }

        ITableConfig LibraryRoot { get; }

        ITableConfig LibraryItem { get; }

        ITableConfig LibraryHierarchy { get; }

        ITableConfig LibraryHierarchyLevel { get; }

        ITableConfig LibraryHierarchyNode { get; }

        ITableConfig MetaDataProvider { get; }
    }
}
