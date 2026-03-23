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
            using (var context = this.AIRuntime.CreateContext())
            {
                var fileId = default(string);
                var vectorStoreId = default(string);
                using (var stream = await this.GetEntireLibrary().ConfigureAwait(false))
                {
                    var store = context.CreateFileStore();
                    fileId = await store.Create(stream, "library.txt").ConfigureAwait(false);
                }
                {
                    var store = context.CreateVectorStore();
                    vectorStoreId = await store.Create("library.txt").ConfigureAwait(false);
                    await store.AddFile(vectorStoreId, fileId).ConfigureAwait(false);
                }
                {
                    var store = context.CreateResponseStore();
                    var result = await store.Create(string.Format("Create a playlist from my library using the prompt: {0}. Ensure that the output is in valid CSV format containing only the file name without headers.", this.Prompt), vectorStoreId);
                    await this.AddPlaylistItems(result).ConfigureAwait(false);
                }
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

        private async Task<Stream> GetEntireLibrary()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("\"FileName\",\"Name\",\"Value\"").ConfigureAwait(false);
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var reader = this.GetEntireLibrary(transaction))
                {
                    using (var sequence = reader.GetAsyncEnumerator())
                    {
                        while (await sequence.MoveNextAsync().ConfigureAwait(false))
                        {
                            writer.WriteLine(string.Concat(
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
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
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
