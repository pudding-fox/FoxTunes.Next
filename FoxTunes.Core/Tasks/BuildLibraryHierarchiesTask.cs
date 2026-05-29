using FoxDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BuildLibraryHierarchiesTask : LibraryTaskBase
    {
        new public const string ID = "8C8CCEFC-A83E-438A-A365-9383DA23E08E";

        public BuildLibraryHierarchiesTask() : base(ID)
        {

        }

        public BuildLibraryHierarchiesTask(IEnumerable<LibraryItem> libraryItems)
            : this()
        {
            this.LibraryItems = libraryItems;
        }

        public IEnumerable<LibraryItem> LibraryItems { get; private set; }

        public override bool Visible
        {
            get
            {
                if (this.LibraryItems == null)
                {
                    return true;
                }
                else
                {
                    return this.LibraryItems.Count() > 512;
                }
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        protected override async Task OnStarted()
        {
            this.Name = "Building hierarchies";
            this.Description = "Preparing";
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            if (this.LibraryItems == null)
            {
                await this.RemoveHierarchies(default(LibraryItemStatus)).ConfigureAwait(false);
                var libraryItems = default(IEnumerable<LibraryItem>);
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var set = this.Database.Set<LibraryItem>(transaction);
                    libraryItems = set.ToArray();
                }
                await this.BuildHierarchies(libraryItems).ConfigureAwait(false);
            }
            else
            {
                await this.RemoveHierarchies(this.LibraryItems).ConfigureAwait(false);  
                await this.BuildHierarchies(this.LibraryItems).ConfigureAwait(false);
            }
        }
    }
}
