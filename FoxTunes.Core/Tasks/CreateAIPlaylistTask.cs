using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class CreateAIPlaylistTask : PlaylistTaskBase
    {
        public CreateAIPlaylistTask(Playlist playlist, string prompt) : base(playlist, 0)
        {
            this.Prompt = prompt;
        }

        public string Prompt { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public IAIRuntime AIRuntime { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.AIRuntime = core.Components.AIRuntime;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            await this.AddPlaylistItems().ConfigureAwait(false);
            await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
        }

        private async Task AddPlaylistItems()
        {
            this.Name = "Creating playlist";
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    await this.AddPlaylistItems(transaction).ConfigureAwait(false);
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        private async Task AddPlaylistItems(ITransactionSource transaction)
        {
            var preface = new[]
            {
                this.AIRuntime.CorePrompts.AllTracks
            };
            using (var context = this.AIRuntime.CreateContext(preface))
            {
                var response = await context.Chat(this.Prompt);
            }
        }
    }
}
