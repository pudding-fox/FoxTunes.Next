using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryMetaDataPopulator : MetaDataPopulator
    {
        public LibraryMetaDataPopulator(IDatabaseComponent database, bool reportProgress, ITransactionSource transaction) : base(database, database.Queries.AddLibraryMetaDataItem, reportProgress, transaction)
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

        public async Task Populate(LibraryItemStatus libraryItemStatus, CancellationToken cancellationToken)
        {
            var query = this.Database
                .AsQueryable<LibraryItem>(this.Database.Source(new DatabaseQueryComposer<LibraryItem>(this.Database), this.Transaction))
                .Where(libraryItem => libraryItem.Status == libraryItemStatus && !libraryItem.MetaDatas.Any());
            await this.Populate(query, cancellationToken).ConfigureAwait(false);
            var populator = new LibraryVariousArtistsPopulator(this.Database);
            if (this.DetectCompilations.Value)
            {
                await populator.Populate(libraryItemStatus, this.Transaction).ConfigureAwait(false);
            }
            else
            {
                await populator.Clear(libraryItemStatus, this.Transaction).ConfigureAwait(false);
            }
        }
    }
}
