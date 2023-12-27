using FoxDb.Interfaces;

namespace FoxTunes.Templates
{
    public partial class GetPlaylistMetaData
    {
        public GetPlaylistMetaData(IDatabase database, int count, int limit)
        {
            this.Database = database;
            this.Count = count;
            this.Limit = limit;
        }

        public IDatabase Database { get; private set; }

        public int Count { get; private set; }

        public int Limit { get; private set; }
    }
}
