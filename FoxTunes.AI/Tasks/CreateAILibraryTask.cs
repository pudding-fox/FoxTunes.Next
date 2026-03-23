using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes.AI.Tasks
{
    public class CreateAILibraryTask : BackgroundTask
    {
        public const string ID = "F50322A9-06A4-43EA-9659-F7C76B1F8A5C";

        public CreateAILibraryTask(string fileId, string vectorStoreId) : base(ID)
        {
            this.FileId = fileId;
            this.VectorStoreId = vectorStoreId;
        }

        public string FileId { get; private set; }

        public string VectorStoreId { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IAIRuntime Runtime { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            this.Runtime = core.Components.AIRuntime;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            using (var context = this.Runtime.CreateContext())
            {
                if (!string.IsNullOrEmpty(this.FileId))
                {
                    var store = context.CreateFileStore();
                    await store.Delete(this.FileId).ConfigureAwait(false);
                }
                if (!string.IsNullOrEmpty(this.VectorStoreId))
                {
                    var store = context.CreateVectorStore();
                    await store.Delete(this.VectorStoreId).ConfigureAwait(false);
                }
                using (var stream = await this.GetEntireLibrary().ConfigureAwait(false))
                {
                    var store = context.CreateFileStore();
                    this.FileId = await store.Create(stream, "library.txt").ConfigureAwait(false);
                }
                {
                    var store = context.CreateVectorStore();
                    this.VectorStoreId = await store.Create("library.txt").ConfigureAwait(false);
                    await store.AddFile(this.VectorStoreId, this.FileId).ConfigureAwait(false);
                }
            }
        }

        private async Task<Stream> GetEntireLibrary()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("\"FileName\",\"Name\",\"Value\"").ConfigureAwait(false);
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = this.GetEntireLibrary(database, transaction))
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
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        protected virtual IDatabaseReader GetEntireLibrary(IDatabaseComponent database, ITransactionSource transaction)
        {
            return database.ExecuteReader(database.Queries.GetEntireLibrary, (parameters, phase) =>
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