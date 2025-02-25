using System.Threading.Tasks;

namespace FoxTunes
{
    public class ClearLibraryHierarchiesTask : LibraryTaskBase
    {
        public ClearLibraryHierarchiesTask()
            : base()
        {

        }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        protected override async Task OnStarted()
        {
            this.Name = "Clearing hierarchies";
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override Task OnRun()
        {
            return this.RemoveHierarchies(null);
        }

        protected override async Task OnCompleted()
        {
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.HierarchiesUpdated)).ConfigureAwait(false);
            await base.OnCompleted().ConfigureAwait(false);
        }
    }
}
