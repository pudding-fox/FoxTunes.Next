using System.Threading.Tasks;

namespace FoxTunes
{
    public class ClearLibraryHierarchiesTask : LibraryTaskBase
    {
        public ClearLibraryHierarchiesTask(LibraryItemStatus? status, bool signal)
            : base()
        {
            this.Status = status;
            this.Signal = signal;
        }

        public LibraryItemStatus? Status { get; private set; }

        public bool Signal { get; private set; }

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
            return this.RemoveHierarchies(this.Status);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            if (this.Signal)
            {
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.HierarchiesUpdated)).ConfigureAwait(false);
            }
        }
    }
}
