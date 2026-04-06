using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryMetaDataPopulator : MetaDataPopulator
    {
        public LibraryMetaDataPopulator(IDatabaseComponent database, bool reportProgress, ITransactionSource transaction) : base(database, database.Queries.AddLibraryMetaDataItem, reportProgress, transaction)
        {

        }

        public Task<IEnumerable<LibraryItem>> Populate(LibraryItemStatus libraryItemStatus, CancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 12;
            var query = this.Database
                .AsQueryable<LibraryItem>(this.Database.Source(new DatabaseQueryComposer<LibraryItem>(this.Database), this.Transaction))
                .Where(libraryItem => libraryItem.Status == libraryItemStatus && !libraryItem.MetaDatas.Any())
                .ToArray() //TODO: This is really bad.
                .GroupBy(libraryItem => libraryItem.DirectoryName, StringComparer.OrdinalIgnoreCase)
                .Take(BATCH_SIZE + 1);
            return this.Populate(query, BATCH_SIZE, cancellationToken);
        }
    }
}
