using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryVariousArtistsPopulator
    {
        public LibraryVariousArtistsPopulator(IDatabaseComponent database)
        {
            this.Database = database;
        }

        public IDatabaseComponent Database { get; private set; }

        public Task Populate(ITransactionSource transaction)
        {
            return this.Database.ExecuteAsync(this.Database.Queries.UpdateLibraryVariousArtists, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["name"] = CustomMetaData.VariousArtists;
                        parameters["type"] = MetaDataItemType.Tag;
                        parameters["value"] = bool.TrueString;
                        break;
                }
            }, transaction);
        }

        public Task Clear(ITransactionSource transaction)
        {
            return this.Database.ExecuteAsync(this.Database.Queries.RemoveLibraryVariousArtists, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["name"] = CustomMetaData.VariousArtists;
                        parameters["type"] = MetaDataItemType.Tag;
                        break;
                }
            }, transaction);
        }
    }
}
