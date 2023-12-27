#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class DatabaseTables : BaseComponent, IDatabaseTables
    {
        public DatabaseTables(IDatabaseComponent database)
        {
            this.Database = database;
        }

        public IDatabaseComponent Database { get; private set; }

        public ITableConfig MetaDataItem { get; private set; }

        public ITableConfig PlaylistItem { get; private set; }

        public ITableConfig PlaylistColumn { get; private set; }

        public ITableConfig LibraryRoot { get; private set; }

        public ITableConfig LibraryItem { get; private set; }

        public ITableConfig LibraryHierarchy { get; private set; }

        public ITableConfig LibraryHierarchyLevel { get; private set; }

        public ITableConfig LibraryHierarchyNode { get; private set; }

        public ITableConfig MetaDataProvider { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataItem = this.Database.Config.Table<MetaDataItem>();
            this.PlaylistItem = this.Database.Config.Table<PlaylistItem>();
            this.PlaylistColumn = this.Database.Config.Table<PlaylistColumn>();
            this.LibraryRoot = this.Database.Config.Table<LibraryRoot>();
            this.LibraryItem = this.Database.Config.Table<LibraryItem>();
            this.LibraryHierarchy = this.Database.Config.Table<LibraryHierarchy>();
            this.LibraryHierarchyLevel = this.Database.Config.Table<LibraryHierarchyLevel>();
            this.LibraryHierarchyNode = this.Database.Config.Table<LibraryHierarchyNode>();
            this.MetaDataProvider = this.Database.Config.Table<MetaDataProvider>();
            base.InitializeComponent(core);
        }
    }
}
