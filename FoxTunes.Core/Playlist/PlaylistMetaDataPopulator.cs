using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class PlaylistMetaDataPopulator : MetaDataPopulator
    {
        public PlaylistMetaDataPopulator(IDatabaseComponent database, bool reportProgress, ITransactionSource transaction) : base(database, database.Queries.AddPlaylistMetaDataItem, reportProgress, transaction)
        {

        }

        public Task Populate(PlaylistItemStatus playlistItemStatus, CancellationToken cancellationToken)
        {
            var query = this.Database
               .AsQueryable<PlaylistItem>(this.Database.Source(new DatabaseQueryComposer<PlaylistItem>(this.Database), this.Transaction))
               .Where(playlistItem => playlistItem.Status == playlistItemStatus && playlistItem.LibraryItem_Id == null)
               .GroupBy(playlistItem => playlistItem.DirectoryName);
            return this.Populate(query, cancellationToken);
        }
    }
}
