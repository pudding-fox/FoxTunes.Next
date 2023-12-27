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

        public BooleanConfigurationElement DetectCompilations { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.DetectCompilations = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.DETECT_COMPILATIONS
            );
        }

        public async Task Populate(PlaylistItemStatus playlistItemStatus, CancellationToken cancellationToken)
        {
            var query = this.Database
               .AsQueryable<PlaylistItem>(this.Database.Source(new DatabaseQueryComposer<PlaylistItem>(this.Database), this.Transaction))
               .Where(playlistItem => playlistItem.Status == playlistItemStatus && playlistItem.LibraryItem_Id == null);
            await this.Populate(query, cancellationToken).ConfigureAwait(false);
            var populator = new PlaylistVariousArtistsPopulator(this.Database);
            if (this.DetectCompilations.Value)
            {
                await populator.Populate(playlistItemStatus, this.Transaction).ConfigureAwait(false);
            }
            else
            {
                await populator.Clear(playlistItemStatus, this.Transaction).ConfigureAwait(false);
            }
        }
    }
}
