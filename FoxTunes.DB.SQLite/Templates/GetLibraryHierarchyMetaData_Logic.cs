using FoxDb.Interfaces;
using FoxTunes.Interfaces;

namespace FoxTunes.Templates
{
    public partial class GetLibraryHierarchyMetaData
    {
        public GetLibraryHierarchyMetaData(IDatabase database, IFilterParserResult filter, int limit)
        {
            this.Database = database;
            this.Filter = filter;
            this.Limit = limit;
        }

        public IDatabase Database { get; private set; }

        public IFilterParserResult Filter { get; private set; }

        public int Limit { get; private set; }
    }
}
