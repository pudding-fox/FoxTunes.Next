using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
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

        public IAIRuntime Runtime { get; private set; }

        public TextConfigurationElement VectorStoreId { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Runtime = core.Components.AIRuntime;
            this.VectorStoreId = this.Configuration.GetElement<TextConfigurationElement>(
                AILibraryBehaviourConfiguration.SECTION,
                AILibraryBehaviourConfiguration.VECTOR_STORE_ID
            );
            if (string.IsNullOrEmpty(this.VectorStoreId.Value))
            {
                throw new InvalidOperationException("Vector store has not been configured, please check your settings.");
            }
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
            using (var context = this.Runtime.CreateContext())
            {
                var store = context.CreateResponseStore();
                var result = await store.Create(string.Format("Create a playlist from my library using the prompt: {0}. Ensure that the output is in valid CSV format containing only the file name without headers.", this.Prompt), this.VectorStoreId.Value);
                await this.AddPlaylistItems(result).ConfigureAwait(false);
            }
        }

        private async Task AddPlaylistItems(string response)
        {
            using (var reader = new StringReader(response))
            {
                var line = default(string);
                var foundHeader = default(bool);
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("```csv"))
                    {
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    throw new InvalidOperationException("Data could not be located in the response.");
                }
                var paths = new List<string>();
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        break;
                    }
                    paths.Add(line.Trim(new[] { '"', ' ' }));
                }
                await this.AddPlaylistItems(paths, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
