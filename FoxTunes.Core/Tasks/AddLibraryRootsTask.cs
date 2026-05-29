using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddLibraryRootsTask : LibraryTaskBase
    {
        public AddLibraryRootsTask(IEnumerable<string> roots)
        {
            this.Roots = roots;
        }

        public IEnumerable<string> Roots { get; private set; }

        protected override Task OnRun()
        {
            return this.AddRoots(this.Roots);
        }
    }
}
