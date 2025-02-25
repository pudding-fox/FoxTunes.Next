using System.Threading.Tasks;

namespace FoxTunes
{
    public class UpdateVariousArtistsTask : LibraryTaskBase
    {
        public UpdateVariousArtistsTask(bool detectCompilations)
        {
            this.DetectCompilations = detectCompilations;
        }

        public bool DetectCompilations { get; private set; }


        protected override async Task OnRun()
        {
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    var populator = new LibraryVariousArtistsPopulator(this.Database);
                    await populator.Clear(LibraryItemStatus.None, transaction).ConfigureAwait(false);
                    if (this.DetectCompilations)
                    {
                        await populator.Populate(LibraryItemStatus.None, transaction).ConfigureAwait(false);
                    }
                    transaction.Commit();
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
