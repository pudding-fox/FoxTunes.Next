using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BuildLibraryHierarchiesTask : LibraryTaskBase
    {
        new public const string ID = "8C8CCEFC-A83E-438A-A365-9383DA23E08E";

        public BuildLibraryHierarchiesTask(IEnumerable<LibraryItem> libraryItems)
            : base(ID)
        {
            this.LibraryItems = libraryItems;
        }

        public IEnumerable<LibraryItem> LibraryItems { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
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
            await this.BuildHierarchies(this.LibraryItems).ConfigureAwait(false);
        }
    }
}
