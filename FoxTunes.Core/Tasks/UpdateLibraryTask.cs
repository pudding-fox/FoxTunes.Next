using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class UpdateLibraryTask : LibraryTaskBase
    {
        public UpdateLibraryTask(LibraryItemStatus status) : base()
        {
            this.Status = status;
        }

        public UpdateLibraryTask(IEnumerable<LibraryItem> libraryItems, LibraryItemStatus status) : this(status)
        {
            this.LibraryItems = libraryItems;
        }

        public IEnumerable<LibraryItem> LibraryItems { get; private set; }

        public LibraryItemStatus Status { get; private set; }

        protected override async Task OnRun()
        {
            if (this.LibraryItems != null && this.LibraryItems.Any())
            {
                foreach (var libraryItem in this.LibraryItems)
                {
                    libraryItem.Status = this.Status;
                    await UpdateLibraryItem(this.Database, libraryItem).ConfigureAwait(false); ;
                }
            }
            else
            {
                await SetLibraryItemsStatus(this.Database, this.Status).ConfigureAwait(false);
            }
        }
    }
}
