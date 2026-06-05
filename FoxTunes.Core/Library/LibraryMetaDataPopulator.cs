using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryMetaDataPopulator : MetaDataPopulator
    {
        public int BATCH_SIZE = 512;

        public LibraryMetaDataPopulator(IDatabaseComponent database, bool reportProgress, ITransactionSource transaction) : base(database, database.Queries.AddLibraryMetaDataItem, reportProgress, transaction)
        {

        }

        public BooleanConfigurationElement LiveUpdates { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.LiveUpdates = this.Configuration.GetElement<BooleanConfigurationElement>(
                LibraryBehaviourConfiguration.SECTION,
                LibraryBehaviourConfiguration.LIVE_UPDATES
            );
        }

        public Task<IEnumerable<LibraryItem>> Populate(LibraryItemStatus libraryItemStatus, CancellationToken cancellationToken)
        {
            var batchSize = default(int);
            if (this.LiveUpdates.Value)
            {
                batchSize = BATCH_SIZE;
            }
            var query = this.Database
                .AsQueryable<LibraryItem>(this.Database.Source(new DatabaseQueryComposer<LibraryItem>(this.Database), this.Transaction))
                .Where(libraryItem => libraryItem.Status == libraryItemStatus && !libraryItem.MetaDatas.Any());
            if (batchSize > 0)
            {
                query = query.Take(BATCH_SIZE + 1);
            }
            return this.Populate(query, batchSize, cancellationToken);
        }

        protected override string GetName(int count, string eta)
        {
            return string.Format("Populating meta data: {0} items/s", count);
        }
    }
}
