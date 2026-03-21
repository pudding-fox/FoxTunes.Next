using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Text;
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
            var library = await this.GetEntireLibrary().ConfigureAwait(false);
            var prompts = new[]
            {
                new AIPrompt(this.AIRuntime.CorePrompts.AvailableTracks, AIPromptType.Message),
                new AIPrompt(library, AIPromptType.Embedding)
            };
            using (var context = this.AIRuntime.CreateContext(prompts))
            {
                var prompt = this.AIRuntime.CorePrompts.CreatePlaylist(this.Prompt);
                var response = await context.Chat(prompt);
            }
        }

        private async Task<string> GetEntireLibrary()
        {
            var builder = new StringBuilder();
            builder.AppendLine("\"FileName\",\"Name\",\"Value\"");
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var reader = this.GetEntireLibrary(transaction))
                {
                    using (var sequence = reader.GetAsyncEnumerator())
                    {
                        while (await sequence.MoveNextAsync().ConfigureAwait(false))
                        {
                            if (builder.Length > 0)
                            {
                                builder.AppendLine();
                            }
                            builder.Append(string.Concat(
                                "\"",
                                sequence.Current.Get<string>("FileName"),
                                "\", \"",
                                sequence.Current.Get<string>("Name"),
                                "\", \"",
                                sequence.Current.Get<string>("Value"),
                                "\""
                            ));
                        }
                    }
                }
            }
            return builder.ToString();
        }

        protected virtual IDatabaseReader GetEntireLibrary(ITransactionSource transaction)
        {
            return this.Database.ExecuteReader(this.Database.Queries.GetEntireLibrary, (parameters, phase) =>
            {
                switch (phase)
                {
                    case FoxDb.Interfaces.DatabaseParameterPhase.Fetch:
                        //Nothing to do.
                        break;
                }
            }, transaction);
        }
    }
}
