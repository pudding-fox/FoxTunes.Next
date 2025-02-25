using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class PlaylistVariousArtistsPopulator
    {
        public PlaylistVariousArtistsPopulator(IDatabaseComponent database)
        {
            this.Database = database;
        }

        public IDatabaseComponent Database { get; private set; }

        public Task Populate(PlaylistItemStatus playlistItemStatus, ITransactionSource transaction)
        {
            return this.Database.ExecuteAsync(this.Database.Queries.UpdatePlaylistVariousArtists, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["name"] = CustomMetaData.VariousArtists;
                        parameters["type"] = MetaDataItemType.Tag;
                        parameters["value"] = bool.TrueString;
                        parameters["status"] = playlistItemStatus;
                        break;
                }
            }, transaction);
        }

        public Task Clear(PlaylistItemStatus playlistItemStatus, ITransactionSource transaction)
        {
            return this.Database.ExecuteAsync(this.Database.Queries.RemoveLibraryVariousArtists, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["name"] = CustomMetaData.VariousArtists;
                        parameters["type"] = MetaDataItemType.Tag;
                        parameters["status"] = playlistItemStatus;
                        break;
                }
            }, transaction);
        }
    }
}
