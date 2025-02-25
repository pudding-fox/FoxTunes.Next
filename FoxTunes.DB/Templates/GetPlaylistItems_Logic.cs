using FoxDb.Interfaces;
using FoxTunes.Interfaces;

namespace FoxTunes.Templates
{
    public partial class GetPlaylistItems
    {
        public GetPlaylistItems(IDatabase database, IFilterParserResult filter)
        {
            this.Database = database;
            this.Filter = filter;
        }

        public IDatabase Database { get; private set; }

        public IFilterParserResult Filter { get; private set; }
    }
}
